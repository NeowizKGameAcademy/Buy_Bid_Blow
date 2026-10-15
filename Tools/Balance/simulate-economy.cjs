/* Synthetic playtest, not a prediction of human behavior.
   Run: node Tools/Balance/simulate-economy.cjs
   Prices are exported from the actual C# market generator to worlds.json. */
const fs = require('fs');
const dir = 'Docs/Analysis/Economy';
const input = JSON.parse(fs.readFileSync(dir + '/worlds.json', 'utf8').replace(/^﻿/, ''));
const settings = input.Settings;
function rng(seed) {
  let t = seed >>> 0;
  return () => { t += 0x6D2B79F5; let x = t; x = Math.imul(x ^ x >>> 15, x | 1); x ^= x + Math.imul(x ^ x >>> 7, x | 61); return ((x ^ x >>> 14) >>> 0) / 4294967296; };
}
function shuffle(values, random) {
  const a = [...values];
  for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(random() * (i + 1)); [a[i], a[j]] = [a[j], a[i]]; }
  return a;
}
function assert(ok, text) { if (!ok) throw Error(text); }
const mean = a => a.reduce((s, v) => s + v, 0) / a.length;
const median = a => { const s = [...a].sort((x, y) => x - y); return (s[Math.floor((s.length - 1) / 2)] + s[Math.floor(s.length / 2)]) / 2; };
const magnitude = settings.PriceChange.AmountOffsets.map(o => {
  let sum = 0, count = 0;
  for (let a = settings.PriceChange.Amount.Min; a <= settings.PriceChange.Amount.Max; a++)
    for (let b = o.Min; b <= o.Max; b++) { sum += Math.max(1, a + b); count++; }
  return sum / count;
});
const regimes = {
  balanced: { reserve: .20, bidding: .65, bidShare: .24, stockExposure: .75, blindBet: .015 },
  infoFocused: { reserve: .35, bidding: .9, bidShare: .4, stockExposure: .60, blindBet: .04 }
};
function simulate(world, count, initialCash, supplyFactor, regimeName, trace = false) {
  const profile = regimes[regimeName];
  const random = rng(world.Seed ^ 0x345678);
  const supply = Array(3).fill(count * supplyFactor);
  const inventory = [...supply];
  const players = Array.from({ length: count }, (_, id) => ({
    id, cash: initialCash, reserved: 0, shares: [0, 0, 0], intel: new Map(), bet: null,
    purchaseCost: 0, saleRevenue: 0, fees: 0, betProfit: 0, auctionsWon: 0,
    risk: [.85, 1, 1.15][id % 3], random: rng(world.Seed * 101 + id * 379 + 99)
  }));
  const packages = new Map();
  for (const start of settings.AuctionRounds) {
    const targets = shuffle([start, start + 1, start + 2, start + 3], random);
    packages.set(start, [targets.slice(0, 2), targets.slice(2)].map(rounds => rounds.flatMap(round =>
      shuffle([0, 1, 2], random).slice(0, 2).map(company => ({ round, company }))
    )));
  }
  let prices = [...world.StartingPrices];
  const wealth = p => p.cash + p.shares.reduce((s, n, c) => s + n * prices[c], 0);
  const metrics = { buyRequests: 0, rejectedBuys: 0, trades: 0, volume: 0, auctionSold: 0, auctionSum: 0,
    bets: 0, winningBets: 0, cancelledBets: 0, checkpoints: 0, soldOutCells: 0, cashFractionSum: 0 };
  const events = [], timeline = [];
  function audit() {
    for (let c = 0; c < 3; c++)
      assert(inventory[c] >= 0 && inventory[c] + players.reduce((s, p) => s + p.shares[c], 0) === supply[c], 'Share conservation');
    players.forEach(p => {
      assert(Number.isInteger(p.cash) && p.cash >= 0 && p.reserved >= 0 && p.reserved <= p.cash, 'Cash/reservation violation');
      assert(p.shares.every(v => Number.isInteger(v) && v >= 0), 'Invalid shares');
    });
  }
  function trade(round) {
    // All bots see the same current public prices. No bot sees hidden prices or changes.
    for (const p of shuffle(players, random)) {
      const keepCash = Math.min(wealth(p) * profile.reserve, initialCash * profile.reserve * p.risk);
      for (let c = 0; c < 3; c++) {
        const known = p.intel.get(round + ':' + c);
        if (!p.shares[c]) continue;
        const needsCash = p.cash - p.reserved < keepCash;
        const sell = known === -1 || known === 0 ? p.shares[c] :
          needsCash ? Math.min(p.shares[c], Math.ceil((keepCash - (p.cash - p.reserved)) / prices[c])) :
          p.random() < .035 ? 1 : 0;
        if (sell > 0) {
          p.shares[c] -= sell; inventory[c] += sell; p.cash += sell * prices[c]; p.saleRevenue += sell * prices[c];
          metrics.trades++; metrics.volume += sell;
        }
      }
      if (p.bet && (p.intel.get(round + ':' + p.bet.company) >= 0)) {
        p.cash += p.bet.stake; p.bet = null; metrics.cancelledBets++;
      }
      if (!p.bet) {
        const knownDown = [0, 1, 2].filter(c => prices[c] > settings.MinPrice && p.intel.get(round + ':' + c) === -1);
        const company = knownDown.sort((a, b) => prices[b] - prices[a])[0];
        let choice = company;
        if (choice === undefined && p.random() < profile.blindBet) choice = 0;
        if (choice !== undefined && prices[choice] > settings.MinPrice &&
            p.cash - p.reserved >= prices[choice] && prices[choice] <= wealth(p) * .4) {
          p.bet = { company: choice, stake: prices[choice] };
          p.cash -= prices[choice]; metrics.bets++;
        }
      }
    }
    // Orders are formed before the server processes them; competing orders can lose inventory.
    const orders = [];
    for (const p of players) {
      const value = wealth(p);
      const held = p.shares.reduce((sum, n, c) => sum + n * prices[c], 0);
      const keepCash = Math.min(value * profile.reserve, initialCash * profile.reserve * p.risk);
      const budget = Math.floor(Math.min(p.cash - p.reserved - keepCash, value * profile.stockExposure - held));
      if (budget <= 0) continue;
      const scores = [0, 1, 2].map(c => {
        const known = p.intel.get(round + ':' + c);
        let expected = known === 1 ? magnitude[c] : known === -1 ? -magnitude[c] :
          known === 0 ? 0 : (2 * settings.PriceChange.RiseChances[c] - 1) * magnitude[c];
        return { c, score: expected / prices[c] * (.7 + p.random() * .6) * p.risk };
      }).filter(v => v.score > 0 && inventory[v.c] > 0).sort((a, b) => b.score - a.score);
      if (!scores.length) continue;
      const c = scores[0].c;
      const qty = Math.min(inventory[c], Math.floor(budget / prices[c]));
      if (qty > 0) orders.push({ p, c, qty });
    }
    for (const { p, c, qty } of shuffle(orders, random)) {
      metrics.buyRequests++;
      if (inventory[c] < qty || p.cash - p.reserved < qty * prices[c]) { metrics.rejectedBuys++; continue; }
      inventory[c] -= qty; p.shares[c] += qty; p.cash -= qty * prices[c]; p.purchaseCost += qty * prices[c];
      metrics.trades++; metrics.volume += qty;
    }
    audit();
  }
  function prepareAuction(round, secret, targets) {
    const reference = 20 + settings.AuctionRounds.indexOf(round) * 5;
    const bids = [];
    for (const p of shuffle(players, random)) {
      if (p.random() > profile.bidding) continue;
      const available = p.cash - p.reserved;
      // No knowledge of the actual advertised answers is used in bid valuation.
      const desire = reference * (.65 + p.random() * .7) * p.risk;
      const limit = Math.floor(Math.min(available * profile.bidShare, wealth(p) * .18, desire));
      if (limit < 1) continue;
      const amount = secret ? Math.max(1, Math.floor(limit * (.7 + p.random() * .3))) : limit;
      bids.push({ p, amount, order: bids.length });
    }
    bids.sort((a, b) => b.amount - a.amount || a.order - b.order);
    if (!bids.length) return { winner: null, targets, price: 0, round, secret };
    const winner = bids[0].p;
    // Open ascending equilibrium: winner stops at next rival's limit + $1. Secret: first-price.
    const price = secret ? bids[0].amount : Math.min(bids[0].amount, bids[1] ? bids[1].amount + 1 : 1);
    if (secret) bids.forEach(b => { b.p.reserved += b.amount; });
    else winner.reserved += price;
    audit();
    return { winner, targets, price, round, secret, bids };
  }
  function finishAuction(a) {
    if (!a.winner) return;
    if (a.secret) a.bids.forEach(b => { b.p.reserved -= b.amount; });
    else a.winner.reserved -= a.price;
    a.winner.cash -= a.price; a.winner.fees += a.price; a.winner.auctionsWon++;
    for (const entry of a.targets) {
      const prior = entry.round === 1 ? world.StartingPrices : world.Prices[entry.round - 2];
      const close = world.Prices[entry.round - 1];
      a.winner.intel.set(entry.round + ':' + entry.company, Math.sign(close[entry.company] - prior[entry.company]));
    }
    metrics.auctionSold++; metrics.auctionSum += a.price;
    if (trace) events.push({ round: a.round, type: a.secret ? 'Secret' : 'Open', winner: a.winner.id + 1, price: a.price, targets: a.targets });
    audit();
  }
  for (let round = 1; round <= settings.RoundCount; round++) {
    if (packages.has(round)) {
      const [first, second] = packages.get(round);
      const open = prepareAuction(round, false, first);
      trade(round); finishAuction(open);
      const secret = prepareAuction(round, true, second);
      trade(round); finishAuction(secret);
      trade(round); trade(round);
    } else { trade(round); trade(round); }
    const close = world.Prices[round - 1];
    for (const p of players) {
      if (p.bet) {
        const won = close[p.bet.company] < prices[p.bet.company];
        if (won) { p.cash += p.bet.stake * 2; metrics.winningBets++; }
        p.betProfit += p.bet.stake * (won ? 1 : -1);
        p.bet = null;
      }
    }
    prices = [...close];
    audit();
    metrics.checkpoints++;
    metrics.soldOutCells += inventory.filter(v => v === 0).length;
    metrics.cashFractionSum += mean(players.map(p => p.cash / Math.max(1, wealth(p))));
    if (trace) timeline.push({ round, prices: [...prices], inventory: [...inventory], averageCash: mean(players.map(p => p.cash)),
      averageWealth: mean(players.map(wealth)), totalFees: players.reduce((v, p) => v + p.fees, 0),
      players: players.map(p => ({ id: p.id + 1, cash: p.cash, shares: [...p.shares], wealth: wealth(p) })) });
  }
  const results = players.map(p => {
    const stockProfit = p.saleRevenue + p.shares.reduce((s, n, c) => s + n * prices[c], 0) - p.purchaseCost;
    assert(wealth(p) === initialCash + stockProfit + p.betProfit - p.fees, 'Player profit identity');
    return { id: p.id + 1, wealth: wealth(p), cash: p.cash, shares: [...p.shares], stockProfit, betProfit: p.betProfit, auctionSpend: p.fees, wins: p.auctionsWon };
  });
  return { seed: world.Seed, count, initialCash, supplyFactor, regimeName, results, metrics, events, timeline };
}
const scenarios = [[100, 3], [200, 3], [200, 4], [200, 5], [200, 6]];
const summaries = [];
let sampleCandidates = [];
for (const regime of Object.keys(regimes)) for (const count of [3, 6]) for (const [cash, supply] of scenarios) {
  const matches = input.Worlds.map(world => simulate(world, count, cash, supply, regime));
  const players = matches.flatMap(m => m.results);
  const sumMetric = key => matches.reduce((s, m) => s + m.metrics[key], 0);
  const summary = {
    regime, players: count, cash, supply,
    meanFinalWealth: mean(players.map(p => p.wealth)), medianFinalWealth: median(players.map(p => p.wealth)),
    meanStockProfit: mean(players.map(p => p.stockProfit)), meanBetProfit: mean(players.map(p => p.betProfit)),
    meanAuctionSpend: mean(players.map(p => p.auctionSpend)),
    meanAuctionPrice: sumMetric('auctionSum') / Math.max(1, sumMetric('auctionSold')),
    auctionSellRate: sumMetric('auctionSold') / (matches.length * 8),
    rejectedBuyRate: sumMetric('rejectedBuys') / Math.max(1, sumMetric('buyRequests')),
    companySoldOutRate: sumMetric('soldOutCells') / (matches.length * 17 * 3),
    averageCashFraction: sumMetric('cashFractionSum') / (matches.length * 17),
    tradesPerPlayer: sumMetric('trades') / players.length,
    betsPerPlayer: sumMetric('bets') / players.length
  };
  summaries.push(summary);
  if (regime === 'balanced' && count === 3 && cash === 200 && supply === 4) {
    const center = median(matches.map(m => mean(m.results.map(p => p.wealth))));
    sampleCandidates = matches.sort((a, b) => Math.abs(mean(a.results.map(p => p.wealth)) - center) - Math.abs(mean(b.results.map(p => p.wealth)) - center));
  }
}
const representative = simulate(input.Worlds.find(w => w.Seed === sampleCandidates[0].seed), 3, 200, 4, 'balanced', true);
const sensitivity = [];
for (const count of [3, 6]) for (const [cash, supply] of [[100, 3], [200, 4]]) {
  const matches = input.Worlds.map(w => simulate(w, count, cash, supply, 'balanced'));
  sensitivity.push({ players: count, cash, supply, min: Math.min(...matches.flatMap(m => m.results.map(p => p.wealth))),
    max: Math.max(...matches.flatMap(m => m.results.map(p => p.wealth))) });
}
fs.writeFileSync(dir + '/results.json', JSON.stringify({ sampleCountPerScenario: input.Worlds.length, summaries, representative, sensitivity }, null, 2));
const table = (regime, count) => summaries.filter(s => s.regime === regime && s.players === count).map(s => ({
  cash: s.cash, supply: s.supply, final: s.meanFinalWealth.toFixed(1), stockProfit: s.meanStockProfit.toFixed(1),
  betProfit: s.meanBetProfit.toFixed(1), fees: s.meanAuctionSpend.toFixed(1), auctionPrice: s.meanAuctionPrice.toFixed(1),
  sold: (s.auctionSellRate * 100).toFixed(1), rejectedBuys: (s.rejectedBuyRate * 100).toFixed(1),
  soldOut: (s.companySoldOutRate * 100).toFixed(1), cashHeld: (s.averageCashFraction * 100).toFixed(1)
}));
for (const regime of Object.keys(regimes)) for (const count of [3, 6]) {
  console.log(regime + ' / ' + count + ' players'); console.table(table(regime, count));
}
console.log('Representative seed:', representative.seed);
console.log('PASS: 10,000 matches; cash, reservation, finite stock and profit invariants checked.');
