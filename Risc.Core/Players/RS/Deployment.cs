﻿using Risk.Core;
using Risk.Models;

namespace Risk.Players.RS
{
    public class Deployment
    {
        private readonly TurnManager _turnManager;

        public Deployment(TurnManager turnManager)
        {
            _turnManager = turnManager;
        }

        public Country ToCountry { get; set; }
        public int Troops { get; set; }

        public void Execute()
        {
            _turnManager.DeployTroops(ToCountry, Troops);
        }
    }
}