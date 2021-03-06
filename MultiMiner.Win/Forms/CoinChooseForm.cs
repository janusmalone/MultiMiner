﻿using MultiMiner.Engine;
using MultiMiner.Engine.Data;
using MultiMiner.Utility.Forms;
using MultiMiner.Engine.Extensions;
using MultiMiner.Xgminer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace MultiMiner.Win.Forms
{
    public partial class CoinChooseForm : MessageBoxFontForm
    {
        public CryptoCoin SelectedCoin { get; set; }

        private List<CryptoCoin> filteredCoins;
        private readonly List<CryptoCoin> sortedCoins;

        public CoinChooseForm(List<CryptoCoin> knownCoins)
        {
            InitializeComponent();
            sortedCoins = knownCoins.OrderBy(c => c.Name).ToList();
        }

        private void CoinChooseForm_Load(object sender, EventArgs e)
        {
            PopulateAlgorithmCombo();
            if (algoCombo.Items.Count > 0)
                algoCombo.SelectedIndex = 0;
            
            coinCombo.Focus();
        }

        private void PopulateAlgorithmCombo()
        {
            algoCombo.Items.Clear();
            List<CoinAlgorithm> algorithms = MinerFactory.Instance.Algorithms;
            foreach (CoinAlgorithm algorithm in algorithms)
                algoCombo.Items.Add(algorithm.Name.ToSpaceDelimitedWords());           
        }

        private CoinAlgorithm GetSelectedAlgorithm()
        {
            string algorithmName = algoCombo.Text.Replace(" ", String.Empty);
            CoinAlgorithm algorithm = MinerFactory.Instance.GetAlgorithm(algorithmName);
            return algorithm;
        }

        private void PopulateCoinCombo()
        {
            CoinAlgorithm algorithm = GetSelectedAlgorithm();
            filteredCoins = sortedCoins.Where(sc => sc.Algorithm.Equals(algorithm.FullName, StringComparison.OrdinalIgnoreCase)).ToList();

            coinCombo.Text = String.Empty;
            coinCombo.Items.Clear();
            foreach (CryptoCoin sortedCoin in filteredCoins)
                coinCombo.Items.Add(String.Format("{0} ({1})", sortedCoin.Name, sortedCoin.Symbol));

            if (coinCombo.Items.Count > 0)
                coinCombo.SelectedIndex = 0;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            CryptoCoin knownCoin = null;
            if (coinCombo.SelectedIndex >= 0)
                knownCoin = filteredCoins[coinCombo.SelectedIndex];
            
            if (knownCoin == null)
            {
                CryptoCoin newCoin = new CryptoCoin();
                newCoin.Name = coinCombo.Text;
                newCoin.Algorithm = GetSelectedAlgorithm().Name;

                using (CoinEditForm coinEditForm = new CoinEditForm(newCoin))
                {
                    if (coinEditForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        SelectedCoin = newCoin;
                    else
                        DialogResult = System.Windows.Forms.DialogResult.Cancel;
                }
            }
            else
            {
                SelectedCoin = knownCoin;
            }
        }

        private void algoCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            PopulateCoinCombo();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/nwoolls/MultiMiner/wiki/Crypto-Coins");
        }
    }
}
