using System.Security.Cryptography.Xml;

namespace ESPClient.Models
{
    public class Test
    {
        #region Properties

        public int Graph { get; set; }

        public List<bool> Causes { get; set; }

        public List<bool> ExpectedEffects { get; set; }

        public List<bool> ActualEffects { get; set; }

        #endregion

        #region Constructor

        public Test(int graphNumber, List<bool> allCauses, List<bool> allEffects)
        {
            Graph = graphNumber;
            Causes = allCauses;
            ExpectedEffects = allEffects;
            ActualEffects = new List<bool>();
            for (int i = 0; i < allEffects.Count; i++)
                ActualEffects.Add(allEffects[i]);
        }

        #endregion

        #region Methods

        public void InjectFault(int faultNumber)
        {
            #region IoT subsystem faults

            if (Graph == 0)
            {
                // light sensor inverted
                if (faultNumber == 0)
                {
                    Causes[0] = !Causes[0];
                    CalculateResult();
                }

                // movement sensor inverted
                else if (faultNumber == 1)
                {
                    Causes[1] = !Causes[1];
                    CalculateResult();
                }

                // voice sensor inverted
                else if (faultNumber == 2)
                {
                    List<List<bool>> allPossibleCombinations = new List<List<bool>>() { new List<bool>() { true, false, false, false },
                                                                                        new List<bool>() { false, true, false, false },
                                                                                        new List<bool>() { false, false, true, false },
                                                                                        new List<bool>() { false, false, false, true } };
                    if (Causes[2])
                        allPossibleCombinations.RemoveAt(0);
                    else if (Causes[3])
                        allPossibleCombinations.RemoveAt(1);
                    else if (Causes[4])
                        allPossibleCombinations.RemoveAt(2);
                    else
                        allPossibleCombinations.RemoveAt(3);

                    // determine random wrong combination
                    Random random = new Random();
                    List<bool> combination = allPossibleCombinations[random.Next(0, 3)];
                    Causes[2] = combination[0];
                    Causes[3] = combination[1];
                    Causes[4] = combination[2];
                    Causes[5] = combination[3];

                    CalculateResult();
                }

                // day-home broken
                else if (faultNumber == 3)
                {
                    if (Causes[6] && Causes[7])
                    {
                        List<List<bool>> possibleLEDColors = new List<List<bool>>() { new List<bool>() { true, false, false },
                                                                                      new List<bool>() { false, true, false },
                                                                                      new List<bool>() { false, false, true } };

                        if (Causes[0])
                            possibleLEDColors.RemoveAt(0);
                        else if (!Causes[0] && (Causes[2] || Causes[3] || Causes[4]))
                            possibleLEDColors.RemoveAt(1);
                        else
                            possibleLEDColors.RemoveAt(2);

                        Random random = new Random();
                        ActualEffects = possibleLEDColors[random.Next(0, 2)];
                    }
                }

                // day-away broken
                else if (faultNumber == 4)
                {
                    if (Causes[6] && !Causes[7])
                    {
                        List<List<bool>> possibleLEDColors = new List<List<bool>>() { new List<bool>() { true, false, false },
                                                                                      new List<bool>() { false, true, false },
                                                                                      new List<bool>() { false, false, true } };

                        if (Causes[0] && !Causes[1] && (Causes[2] || Causes[3]))
                            possibleLEDColors.RemoveAt(0);
                        else
                            possibleLEDColors.RemoveAt(2);

                        Random random = new Random();
                        ActualEffects = possibleLEDColors[random.Next(0, 2)];
                    }
                }

                // night-home broken
                else if (faultNumber == 5)
                {
                    if (!Causes[6] && Causes[7])
                    {
                        List<List<bool>> possibleLEDColors = new List<List<bool>>() { new List<bool>() { true, false, false },
                                                                                      new List<bool>() { false, true, false },
                                                                                      new List<bool>() { false, false, true } };

                        if (Causes[0] && !Causes[1] && (Causes[2] || Causes[3]))
                            possibleLEDColors.RemoveAt(1);
                        else if (!Causes[0] && !Causes[1] && (Causes[2] || Causes[3]))
                            possibleLEDColors.RemoveAt(0);
                        else
                            possibleLEDColors.RemoveAt(2);

                        Random random = new Random();
                        ActualEffects = possibleLEDColors[random.Next(0, 2)];
                    }
                }

                // night-away broken
                else if (faultNumber == 6)
                {
                    if (!Causes[6] && !Causes[7])
                    {
                        List<List<bool>> possibleLEDColors = new List<List<bool>>() { new List<bool>() { true, false, false },
                                                                                      new List<bool>() { false, true, false },
                                                                                      new List<bool>() { false, false, true } };

                        if (!Causes[0] && !Causes[1] && Causes[2])
                            possibleLEDColors.RemoveAt(0);
                        else
                            possibleLEDColors.RemoveAt(2);

                        Random random = new Random();
                        ActualEffects = possibleLEDColors[random.Next(0, 2)];
                    }
                }
            }

            #endregion

            #region Server-Mobile subsystem faults

            else if (Graph == 1)
            {
                // connection IoT-server broken
                if (faultNumber == 0)
                {
                    Causes[1] = false;

                    CalculateResult();
                }

                // connection server-IoT broken
                else if (faultNumber == 1)
                {
                    Causes[0] = false;

                    CalculateResult();
                }

                // camera broken
                else if (faultNumber == 2)
                {
                    Causes[5] = false;

                    CalculateResult();
                }

                // connection mobile-server broken
                else if (faultNumber == 3)
                {
                    Causes[2] = false;

                    CalculateResult();
                }
                // face detection API broken
                else if (faultNumber == 4)
                {
                    Causes[3] = !Causes[3];

                    CalculateResult();
                }
            }

            #endregion
        }

        /// <summary>
        /// Check if expected and actual effect values are the same
        /// </summary>
        /// <returns></returns>
        public bool TestPasses()
        {
            for (int i = 0; i < ExpectedEffects.Count; i++)
                if (ExpectedEffects[i] != ActualEffects[i])
                    return false;

            return true;
        }

        /// <summary>
        /// Calculate the expected effect values based on current cause values
        /// </summary>
        public void CalculateResult()
        {
            #region IoT

            if (Graph == 0)
            {
                // day-home
                if (Causes[6] && Causes[7])
                {
                    if (Causes[0])
                        ActualEffects = new List<bool>() { true, false, false };
                    else if (!Causes[0] && (Causes[2] || Causes[3] || Causes[4]))
                        ActualEffects = new List<bool>() { false, true, false };
                    else
                        ActualEffects = new List<bool>() { false, false, true };
                }

                // day-away
                else if (Causes[6] && !Causes[7])
                {
                    if (Causes[0] && !Causes[1] && (Causes[2] || Causes[3]))
                        ActualEffects = new List<bool>() { true, false, false };
                    else
                        ActualEffects = new List<bool>() { false, false, true };
                }

                // night-home
                else if (!Causes[6] && Causes[7])
                {
                    if (Causes[0] && !Causes[1] && (Causes[2] || Causes[3]))
                        ActualEffects = new List<bool>() { false, true, false };
                    else if (!Causes[0] && !Causes[1] && (Causes[2] || Causes[3]))
                        ActualEffects = new List<bool>() { true, false, false };
                    else
                        ActualEffects = new List<bool>() { false, false, true };
                }

                // night-away
                else if (!Causes[6] && !Causes[7])
                {
                    if (!Causes[0] && !Causes[1] && Causes[2])
                        ActualEffects = new List<bool>() { true, false, false };
                    else
                        ActualEffects = new List<bool>() { false, false, true };
                }
            }

            #endregion

            #region Server-Mobile

            else
            {
                ActualEffects = new List<bool>() { false, false, false, false, false, false };

                ActualEffects[0] = Causes[0];
                ActualEffects[1] = Causes[3] && Causes[4];
                ActualEffects[2] = Causes[1] || Causes[2];
                ActualEffects[3] = Causes[6];
                ActualEffects[4] = Causes[6];
                ActualEffects[5] = Causes[5];
            }

            #endregion
        }

        #endregion
    }
}
