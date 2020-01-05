﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace RTS_GAME_2019
{
    class GameEngine
    {
        public Map map;
        int remainingRounds;
        int roundCount;
        System.Timers.Timer myTimer;
        RichTextBox killFeed;
        TextBox resTeam0;
        TextBox resTeam1;
        Label winLabel;
        int bonusResourcesTeam0;
        int bonusResourcesTeam1; //Resources which are added when units die

        public GameEngine(int numUnitsToCreate, int numBuildingsToCreate, int numRows, int numCols, int roundLimit, DataGridView grid, System.Timers.Timer timer, RichTextBox killFeed, Label winLabel, TextBox resTeam0, TextBox resTeam1)
        {
            bonusResourcesTeam0 = 0;
            bonusResourcesTeam1 = 0;
            this.winLabel = winLabel;
            this.killFeed = killFeed;
            this.resTeam0 = resTeam0;
            this.resTeam1 = resTeam1;
            roundCount = 0;
            myTimer = timer;
            remainingRounds = roundLimit;
            map = new Map(numUnitsToCreate, numBuildingsToCreate, numRows, numCols);
            map.GenerateMap(grid);


        }

        public void StartTimer()
        {
            myTimer.Enabled = true;
        }

        public void StopTimer()
        {
            myTimer.Enabled = false;
        }

        public bool isTimerRunning()
        {
            return myTimer.Enabled;
        }

        public static void AppendText(RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }

        public int PerformRound()
        {
            if (remainingRounds <= 0)
            {
                //done!!!
                winLabel.Invoke(new Action(() => winLabel.Text = "Round Limit Reached! It's a draw!"));

                return roundCount;
            }

            int totalReasourcesFromBuildingsTeam0 = 0;
            int totalReasourcesFromBuildingsTeam1 = 0;


            foreach (Building b in map.buildingList)
            {
                if (b is ResourceBuilding)
                {
                    ResourceBuilding r = (ResourceBuilding)b;


                    Debug.WriteLine("GENERATING RESOURCES");
                    r.GenerateResourcesForRound();
                    //now resourcesgenerated has been updated
                    if (r.Team == 0)
                    {
                        totalReasourcesFromBuildingsTeam0 += r.ResourcesGenerated;
                    }
                    else if (r.Team == 1)
                    {
                        totalReasourcesFromBuildingsTeam1 += r.ResourcesGenerated;
                    }
                }
            }
            resTeam0.Invoke(new Action(() => resTeam0.Text = "" + (totalReasourcesFromBuildingsTeam0 + bonusResourcesTeam0)));
            resTeam1.Invoke(new Action(() => resTeam1.Text = "" + (totalReasourcesFromBuildingsTeam1 + bonusResourcesTeam1)));

            //first the factories should spawn units then the round battle commenses 
            for (int i =0; i < map.buildingList.Count; i++)
            {
                Building b = map.buildingList.ElementAt(i);
                if (b is FactoryBuilding)
                {
                    FactoryBuilding f = (FactoryBuilding)b;
                    int prodSpeed = f.GetProductionSpeed();
                    if (roundCount % prodSpeed == 0)
                    {
                        if (b.Team == 0)
                        {
                            if (totalReasourcesFromBuildingsTeam0 - 5 >= 0)
                            {
                                //factory may produce a unit
                                f.SpawnUnit(map);
                                totalReasourcesFromBuildingsTeam0 = totalReasourcesFromBuildingsTeam0 - 5;
                                killFeed.Invoke(new Action(() => AppendText(killFeed, "\nBuilding of team 0 spawned a unit", Color.Blue)));
                                killFeed.Invoke(new Action(() => killFeed.ScrollToCaret()));
                            }
                            else if ((totalReasourcesFromBuildingsTeam0 + bonusResourcesTeam0 - 5) >= 0)
                            {
                                //we are able to spawn units because of the added bonus
                                int toTakeFromBonus = 5 - totalReasourcesFromBuildingsTeam0;
                                totalReasourcesFromBuildingsTeam0 = 0;
                                bonusResourcesTeam0 -= toTakeFromBonus;
                                f.SpawnUnit(map);
                                killFeed.Invoke(new Action(() => AppendText(killFeed, "\nBuilding of team 1 spawned a unit", Color.Red)));
                                killFeed.Invoke(new Action(() => killFeed.ScrollToCaret()));
                            }
                        }
                        else if (b.Team == 1)
                        {
                            if (totalReasourcesFromBuildingsTeam1 - 5 >= 0)
                            {
                                //factory may produce a unit
                                f.SpawnUnit(map);
                                totalReasourcesFromBuildingsTeam1 -= 5;
                                killFeed.Invoke(new Action(() => AppendText(killFeed, "\nBuilding of team 1 spawned a unit", Color.Red)));
                                killFeed.Invoke(new Action(() => killFeed.ScrollToCaret()));
                            }
                            else if ((totalReasourcesFromBuildingsTeam1 + bonusResourcesTeam1 - 5) >= 0)
                            {
                                //we are able to spawn units because of the added bonus
                                int toTakeFromBonus = 5 - totalReasourcesFromBuildingsTeam1;
                                totalReasourcesFromBuildingsTeam1 = 0;
                                bonusResourcesTeam1 -= toTakeFromBonus;
                                f.SpawnUnit(map);
                                killFeed.Invoke(new Action(() => AppendText(killFeed, "\nBuilding of team 1 spawned a unit", Color.Red)));
                                killFeed.Invoke(new Action(() => killFeed.ScrollToCaret()));
                            }
                        }
                    }

                }

                killFeed.Invoke(new Action(() => AppendText(killFeed, "\n" + b.ToString(), ((b.Team == 0) ? Color.Blue : Color.Red))));
                killFeed.Invoke(new Action(() => killFeed.ScrollToCaret()));
            }
            resTeam0.Invoke(new Action(() => resTeam0.Text = "" + (totalReasourcesFromBuildingsTeam0 + bonusResourcesTeam0)));
            resTeam1.Invoke(new Action(() => resTeam1.Text = "" + (totalReasourcesFromBuildingsTeam1 + bonusResourcesTeam1)));

            List<Unit> unitList = map.GetUnitList();

            for (int i = 0; i < unitList.Count; i++)
            {
                Unit u = unitList.ElementAt(i);

                if (u is Wizard)
                {
                    //special wizard case
                    if ((u.Health / (double)u.MaxHealth) * 100.0 <= 50)
                    {
                        map.MoveUnitRandomly(u);
                        continue;
                    }

                    // perform special attack
                    List<Unit> possibleUnitsToAttack = new List<Unit>();
                    if (map.IsInMap(u.XPos - 1, u.YPos - 1))
                    {
                        possibleUnitsToAttack.Add(map.unitMap[u.XPos - 1, u.YPos - 1]); //topLeft
                    }

                    if (map.IsInMap(u.XPos - 1, u.YPos))
                    {
                        possibleUnitsToAttack.Add(map.unitMap[u.XPos - 1, u.YPos]);//toptop
                    }

                    if (map.IsInMap(u.XPos - 1, u.YPos + 1))
                    {
                        possibleUnitsToAttack.Add(map.unitMap[u.XPos - 1, u.YPos + 1]); //topRight
                    }

                    if (map.IsInMap(u.XPos, u.YPos + 1))
                    {
                        possibleUnitsToAttack.Add(map.unitMap[u.XPos, u.YPos + 1]); // rightRight
                    }

                    if (map.IsInMap(u.XPos + 1, u.YPos + 1))
                    {
                        possibleUnitsToAttack.Add(map.unitMap[u.XPos + 1, u.YPos + 1]);//bottomRight
                    }

                    if (map.IsInMap(u.XPos + 1, u.YPos))
                    {
                        possibleUnitsToAttack.Add(map.unitMap[u.XPos + 1, u.YPos]); // bottom bottom
                    }

                    if (map.IsInMap(u.XPos + 1, u.YPos - 1))
                    {
                        possibleUnitsToAttack.Add(map.unitMap[u.XPos + 1, u.YPos - 1]); // bottom left
                    }

                    if (map.IsInMap(u.XPos, u.YPos - 1))
                    {
                        possibleUnitsToAttack.Add(map.unitMap[u.XPos, u.YPos - 1]); // left left
                    }

                    //now check if there are targets at these positions
                   
                    bool hasAttacked = false;
                    foreach (Unit victim in possibleUnitsToAttack)
                    {
                        if (victim != null)
                        {
                            if (victim.Team == 0 || victim.Team == 1)
                            {
                                hasAttacked = true;
                                //blow things up
                                bool unitIsNotDead = u.AttackUnit(victim, map);
                                if (!unitIsNotDead)
                                {
                                    //unit died
                                    if (victim.Team == 0)
                                    {
                                        bonusResourcesTeam0++;
                                    }
                                    else if (victim.Team == 1)
                                    {
                                        bonusResourcesTeam1++;
                                    }

                                    for(int j =0; j < unitList.Count; j++)
                                    {
                                        //scan for the victim
                                        if(unitList.ElementAt(j) == victim)
                                        {
                                            unitList.RemoveAt(j);
                                            if(j < i)
                                            {
                                                i--; // removing the victim from the list should not affect the iteration
                                            }
                                        }
                                    }

                                }
                            }
                        }
                    }

                    if (!hasAttacked)
                    {
                        Position closestEnemyPos = u.FindClosestUnitOrBuilding(map); // in this case wizard can only find units
                        if(closestEnemyPos.x == -1)
                        {
                            //wizards have won!!
                            Debug.WriteLine("Team " + u.Team + " WINS!!! -- no enemies left");
                            winLabel.Invoke(new Action(() => winLabel.Text = "Wizards have won the game!!!"));
                            StopTimer();
                            return roundCount;
                        }
                        else
                        {
                            //move towards nearest enemy unit
                            map.MoveTowardsEnemy(u, closestEnemyPos.x, closestEnemyPos.y);

                        }
                    }
                    continue; // done with wizard
                }

                //Case 1: below health so run away
                if ((u.Health / (double)u.MaxHealth) * 100.0 <= 25)
                {
                    map.MoveUnitRandomly(u);
                    continue;
                }

                //Case 2: Finding enemy and decide on attacking

                //Unit closestEnemy = u.FindClosestUnitOrBuilding(map);
                Position closestEnemyPosition = u.FindClosestUnitOrBuilding(map);
                if (closestEnemyPosition.x != -1)
                {
                    // the enemy can be a building or a unit
                    bool isEnemyAUnit = false;
                    if (map.unitMap[closestEnemyPosition.x, closestEnemyPosition.y] != null)
                    {
                        //we know a the enemy is a unit and not a building
                        isEnemyAUnit = true;
                    }

                    if (isEnemyAUnit)
                    {
                        Unit closestEnemy = map.unitMap[closestEnemyPosition.x, closestEnemyPosition.y];
                        if (map.IsWithinRange(u, closestEnemy))
                        {
                            // we can attack
                            bool unitIsNotDead = u.AttackUnit(closestEnemy, map);
                            if (!unitIsNotDead)
                            {
                                //unit died so add resources to its pool
                                if (closestEnemy.Team == 0)
                                {
                                    bonusResourcesTeam0++;
                                }
                                else if (closestEnemy.Team == 1)
                                {
                                    bonusResourcesTeam1++;
                                }

                                for (int j = 0; j < unitList.Count; j++)
                                {
                                    //scan for the victim
                                    if (unitList.ElementAt(j) == closestEnemy)
                                    {
                                        unitList.RemoveAt(j);
                                        if (j < i)
                                        {
                                            i--; // removing the victim from the list should not affect the iteration
                                        }
                                    }
                                }

                            }
                        }
                        else
                        {
                            //we cant attack so move towards
                            map.MoveTowardsEnemy(u, closestEnemyPosition.x, closestEnemyPosition.y);
                        }
                    }
                    else
                    {
                        //There must be a building to attack at this position
                        Building buildingToAttack = null;
                        foreach (Building b in map.buildingList)
                        {
                            if (b.XPos == closestEnemyPosition.x && b.YPos == closestEnemyPosition.y)
                            {
                                buildingToAttack = b;
                            }
                        }

                        if (map.IsWithinRange(u, buildingToAttack))
                        {
                            // we can attack the building
                            bool buildingIsNotDead = u.AttackBuilding(buildingToAttack, map); // the deathhandler will deal with a building's death. No need to worry about it here
                            if (!buildingIsNotDead)
                            {
                                killFeed.Invoke(new Action(() => AppendText(killFeed, "\nBuilding of team " + buildingToAttack.Team + " was destroyed!", ((buildingToAttack.Team == 0) ? Color.Blue : Color.Red))));
                                killFeed.Invoke(new Action(() => killFeed.ScrollToCaret()));
                            }


                        }
                        else
                        {
                            //we cant attack so move towards the building
                            map.MoveTowardsEnemy(u, closestEnemyPosition.x, closestEnemyPosition.y);
                        }
                    }
                    //add units info to the killfeed
                    killFeed.Invoke(new Action(() => AppendText(killFeed, "\n" + u.ToString(), ((u.Team == 0) ? Color.Blue : Color.Red))));
                    killFeed.Invoke(new Action(() => killFeed.ScrollToCaret()));
                }
                else
                {
                    Debug.WriteLine("Team " + u.Team + " WINS!!! -- no enemies left");
                    winLabel.Invoke(new Action(() => winLabel.Text = "Team " + u.Team + " has won the game!"));
                    StopTimer();
                    return roundCount;
                }
            }
            //now round is done
            roundCount++;
            remainingRounds--;
            return roundCount;
        }

    }


}
