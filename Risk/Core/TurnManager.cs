﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Risk.Models;
using Risk.Models.Actions;

namespace Risk.Core
{
    public class TurnManager
    {
        private readonly GameManager gameManager;
        private readonly IPlayer player;
        private readonly Random r;

        public TurnManager(IPlayer player)
        {
            gameManager = GameManager.Get();
            this.player = player;
            r = new Random();
        }

        public void DeployTroops(Country country, int numberOfTroops)
        {
            ValidatePlayer();
            if (gameManager.CurrentPhase != EPhase.Deploy) throw new RiskException("Its not the deploy phase");
            if (country.Owner != player) throw new RiskException("Troops can only be deployed on owned countries");
            if (gameManager.TroopsToDeploy - numberOfTroops < 0) throw new RiskException("Unable to deploy more troops than given");

            gameManager.TroopsToDeploy -= numberOfTroops;
            country.NumberOfTroops += numberOfTroops;

            new ActionLogger().AddDeployAction(country, numberOfTroops);
        }

        public bool Attack(Country country, Country countryToAttack, int numberOftroopsToAttackWith)
        {
            ValidatePlayer();
            if (gameManager.CurrentPhase != EPhase.Attack) throw new RiskException("Its not the attack phase");
            if (country.Owner != player) throw new RiskException("Attacks can only start from a owned country");
            if (countryToAttack.Owner == player) throw new RiskException("Can't attack a owned country");
            if (!country.AdjacentCountries.Contains(countryToAttack)) throw new RiskException("Countries must be adjacent to attack");
            if (country.NumberOfTroops - numberOftroopsToAttackWith < 1) throw new RiskException("Not enough troops on country");

            country.NumberOfTroops -= numberOftroopsToAttackWith;

            var attackAction = new AttackAction()
            {
                Player = country.Owner,
                Country = country,
                CountryToAttack = countryToAttack,
                DefendingPlayer = countryToAttack.Owner,
                Attackers = numberOftroopsToAttackWith,
                Defenders = countryToAttack.NumberOfTroops
            };

            int totalDeadDefenders = 0;
            int totalDeadAttackers = 0;

            do
            {
                int numberOfAttackDiceRolls = Math.Min(3, numberOftroopsToAttackWith);
                List<int> attackRolls = gameManager.GetDiceRolls(numberOfAttackDiceRolls);

                int numberOfDiceToDefendWith;

                try
                {
                    numberOfDiceToDefendWith = countryToAttack.Owner.Defend(attackRolls.GetRange(0, attackRolls.Count), countryToAttack);

                    if (numberOfDiceToDefendWith > 2 || numberOfDiceToDefendWith < 1 || countryToAttack.NumberOfTroops == 1)
                    {
                        numberOfDiceToDefendWith = 1;
                    }
                }
                catch (Exception e)
                {
                    new ActionLogger().AddException(e, countryToAttack.Owner);
                    numberOfDiceToDefendWith = 1;
                }


                List<int> defendRolls = gameManager.GetDiceRolls(numberOfDiceToDefendWith);

                int rollsToCheck = attackRolls.Count > 1 && defendRolls.Count > 1 ? 2 : 1;
                int deadAttackers = 0;
                int deadDefenders = 0;

                for (int i = 0; i < rollsToCheck; i++)
                {
                    int highestAttackRoll = attackRolls.OrderByDescending(roll => roll).First();
                    attackRolls.Remove(highestAttackRoll);

                    int highestDefendRoll = defendRolls.OrderByDescending(roll => roll).First();
                    defendRolls.Remove(highestDefendRoll);

                    if (highestAttackRoll > highestDefendRoll)
                    {
                        deadDefenders++;
                    }
                    else
                    {
                        deadAttackers++;
                    }
                }

                countryToAttack.NumberOfTroops -= deadDefenders;
                numberOftroopsToAttackWith -= deadAttackers;
                totalDeadAttackers += deadAttackers;
                totalDeadDefenders += deadDefenders;

            } while (numberOftroopsToAttackWith > 0 && countryToAttack.NumberOfTroops > 0);

            attackAction.DeadAttackers = totalDeadAttackers;
            attackAction.DeadDefenders = totalDeadDefenders;
            attackAction.AttackSucceeded = countryToAttack.NumberOfTroops == 0;

            gameManager.Actions.Add(attackAction);

            if (countryToAttack.NumberOfTroops == 0)
            {
                countryToAttack.Owner = country.Owner;
                countryToAttack.NumberOfTroops = numberOftroopsToAttackWith;

                return true;
            }

            return false;
        }

        public void MoveTroops(Country country, Country countryToMoveTo, int numberOfTroops)
        {
            ValidatePlayer();
            if (gameManager.CurrentPhase != EPhase.Move) throw new RiskException("Its not the move phase");
            if (country.Owner != player) throw new RiskException("Troops can only be moved from owned countries");
            if (countryToMoveTo.Owner != player) throw new RiskException("Troops can only be moved to owned countries");
            if (!country.AdjacentCountries.Contains(countryToMoveTo)) throw new RiskException("Countries must be adjacent to move");
            if (country.NumberOfTroops - numberOfTroops < 1) throw new RiskException("Not enough troops on country");
            if (gameManager.TroopsToMove - numberOfTroops < 0) throw new RiskException(string.Format("unable to move {0} troops, only {1} moves left", numberOfTroops, gameManager.TroopsToMove));
            if (numberOfTroops < 1) throw new RiskException("Unable to move less then 1 troop");

            gameManager.TroopsToMove -= numberOfTroops;

            country.NumberOfTroops -= numberOfTroops;
            countryToMoveTo.NumberOfTroops += numberOfTroops;

            new ActionLogger().AddMoveAction(country, countryToMoveTo, numberOfTroops);
        }

        private void ValidatePlayer()
        {
            if (gameManager.CurrentPlayer != player) throw new RiskException("Not your turn");
        }

    }

    public enum EPhase
    {
        Deploy,
        Attack,
        Move
    }
}