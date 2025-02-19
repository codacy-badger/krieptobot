﻿using KrieptoBod.Application.Recommendators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KrieptoBod.Application
{
    public class Trader : ITrader
    {

        private readonly IRepository _repository;
        private readonly IRecommendationCalculator _recommendationCalculator;

        public Trader(IRepository repository, IRecommendationCalculator recommendationCalculator)
        {
            _repository = repository;
            _recommendationCalculator = recommendationCalculator;
        }

        public async Task Run()
        {
             var candles = await _repository.GetCandlesAsync("CHZ-EUR");

            var recommendations = await GetRecommendations();

            var coinsToSell = DetermineCoinsToSell(recommendations);
            var coinsToBuy = DetermineCoinsToBuy(recommendations);

            SellCoins(coinsToSell);

            var coinsToBuyWithBudget = GetCoinsToBuyWithBudget(coinsToBuy, recommendations);

            BuyCoins(coinsToBuyWithBudget);
        }

        private static Dictionary<string, float> GetCoinsToBuyWithBudget(IDictionary<string, RecommendatorScore> coinsToBuy, Dictionary<string, RecommendatorScore> recommendations)
        {
            var availableBudgetToInvest = GetAvailableBudgetToInvest(); // todo get from api

            /*
             * example
             * budgetToInvest = 999
             * btc => score 50
             * ada => score 40
             * chz => score 90
             * ltc => score 500
             * => sum of scores = 50+40+90+500 = 680
             * ==> budgets for coins
             * btc => 50 * 999/680 => math.floor 73.4
             * ada => 40 * 999/680 => math.floor 58.7
             * chz => 90 * 999/680 => math.floor 132.2
             * ltc => 500 * 999/680 => math.floor 734.5
             */
            var totalRecommendationScore = coinsToBuy.Sum(x => x.Value.Score);

            return recommendations
                    .Where(x => coinsToBuy.Keys.Contains(x.Key))
                    .ToDictionary(
                        coinToBuyRecommendation => coinToBuyRecommendation.Key,
                        coinToBuyRecommendation => (float)Math.Floor(coinToBuyRecommendation.Value.Score *
                            availableBudgetToInvest / totalRecommendationScore));
        }

        private static float GetAvailableBudgetToInvest()
        {
            return 1000F;
        }

        private void BuyCoins(Dictionary<string, float> coinsToBuyWithBudget)
        {
            foreach (var (coin, budget) in coinsToBuyWithBudget)
            {
                Buy(coin, budget);
            }
        }

        private void SellCoins(IDictionary<string, RecommendatorScore> coinsToSell)
        {
            foreach (var (coin, _) in coinsToSell)
            {
                Sell(coin);
            }
        }

        private static IDictionary<string, RecommendatorScore> DetermineCoinsToBuy(Dictionary<string, RecommendatorScore> recommendations)
        {
            const int buyMargin = 30; // todo app setting
            var coinsToBuy =
                recommendations
                    .Where(x => x.Value.Score >= buyMargin)
                    .ToDictionary(x => x.Key, x => x.Value);

            return coinsToBuy;
        }

        private static IDictionary<string, RecommendatorScore> DetermineCoinsToSell(Dictionary<string, RecommendatorScore> recommendations)
        {
            const int sellMargin = 30; // todo app setting
            var coinsToSell =
                recommendations
                    .Where(x => x.Value.Score <= sellMargin)
                    .ToDictionary(x => x.Key, x => x.Value);

            return coinsToSell;
        }

        private async Task<Dictionary<string, RecommendatorScore>> GetRecommendations()
        {
            return (await GetCoinsToEvaluate()).ToDictionary(coin => coin,
                coin => _recommendationCalculator.CalculateRecommendation(coin));
        }

        private async Task<IEnumerable<string>> GetCoinsToEvaluate()
        {
            var balances = await _repository.GetBalanceAsync();

            var coinsToWatch = new List<string> { "CHZ", "BTC", "ADA" };//todo app settings

            var coinsInPortfolio =
                balances
                    .Where(balance => balance.Available > 0 || balance.InOrder > 0)
                    .Select(balance => balance.Symbol);

            var coinsToEvaluate = coinsInPortfolio.Union(coinsToWatch).ToList();

            return coinsToEvaluate;
        }

        private void Buy(string coin, float amount)
        {
            return;
        }

        private void Sell(string coin)
        {
            return;
        }
    }
}
