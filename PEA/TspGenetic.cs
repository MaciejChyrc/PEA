﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PEA
{
	class TspGenetic
	{
		#region FieldsAndProperties
		private static int numbOfCities; //liczba miast
										 //private static int globalBestDistance;
		private static double globalBestFitness;
		private static List<List<Int16>> population; //populacja - tablica tablic reprezentujacych osobnikow
		private static List<double> fitnessList;
		private static List<Int16> globalBestSolution;
		private static Random rand1, rand2;
		public static int Generations { get; set; }
		public static Int16[][] InputGraph { get; set; } //graf wejsciowy
		public static List<Int16> OutputList { get; private set; } //lista miast odwiedzonych w danej kolejnosci
		public static int PopulationSize { get; set; } //rozmiar populacji
		public static int ElitismSize { get; set; } //ilosc wybranych najlepszych do nowej generacji
		public static int TournamentSize { get; set; } //rozmiar puli wyboru do krzyzowania
		public static double MutationRate { get; set; } //szansa na mutacje
		public static int PathDistance { get; private set; } //dlugosc sciezki
		public static TimeSpan TimeMeasured { get; private set; } //zmierzony czas wykonywania obliczen
		#endregion

		public static void SolveTsp(TspGraph input, int generations, int populationSize, int elitismSize, double mutationRate = 0.05, int tournamentSize = 2)
		{
			if (input.Dimension > 2 && input.Name != null && input.GraphMatrix != null)
			{
				if (elitismSize % 2 == 1)
				{
					elitismSize--;
					Console.WriteLine("Ilość elit powinna być podzielna przez 2. Zmniejszono o 1.");
				}

				bool allright = Init(input, generations, populationSize, elitismSize, tournamentSize, mutationRate);

				if (allright)
				{
					Stopwatch stopwatch = new Stopwatch();
					stopwatch.Start();
					Search();
					stopwatch.Stop();
					TimeMeasured = stopwatch.Elapsed;
				}
			}
			else Console.WriteLine("Przed uruchomieniem algorytmu wczytaj odpowiednio dane wejściowe.\n(Wciśnij dowolny klawisz aby wrócić)");
		}

		private static bool Init(TspGraph input, int generations, int populationSize, int elitismSize, int tournamentSize, double mutationRate)
		{
			try
			{
				InputGraph = input.GraphMatrix;
				numbOfCities = input.Dimension;
				Generations = generations;
				PopulationSize = populationSize;
				ElitismSize = elitismSize;
				TournamentSize = tournamentSize;
				MutationRate = mutationRate;
				//globalBestDistance = int.MaxValue;
				globalBestFitness = 0;
				rand1 = new Random();
				rand2 = new Random();

				//var initial = new List<Int16>(numbOfCities + 1);
				fitnessList = new List<double>(populationSize);

				//for (Int16 i = 0; i < initial.Capacity - 1; i++)
				//	initial.Add(i);
				//initial.Add(0);

				population = new List<List<Int16>>(populationSize);

				for (int i = 0; i < population.Capacity; i++)
				{
					population.Add(new List<short>(numbOfCities + 1));
					for (Int16 j = 0; j < population[i].Capacity - 1; j++)
						population[i].Add(j);
					population[i].Add(0);
					RandomizeSolution(population[i], i);
					fitnessList.Add(CalculateFitness(population[i]));
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Błąd. Opis błędu: " + e);
				return false;
			}

			return true;
		}

		private static void Search()
		{
			for (int i = 0; i < Generations; i++)
			{
				population = NextGeneration();
				Debug.WriteLine("PopulationSize: " + population.Count);
				Debug.WriteLine("Generation: " + i);
				fitnessList = new List<double>(PopulationSize);

				for (int j = 0; j < fitnessList.Capacity; j++)
					fitnessList.Add(CalculateFitness(population[j]));

				int tempMaxIndex = FindFittest(fitnessList);

				if (fitnessList[tempMaxIndex] > globalBestFitness)
				{
					globalBestFitness = fitnessList[tempMaxIndex];
					globalBestSolution = population[tempMaxIndex];
				}
			}

			OutputList = globalBestSolution;
			PathDistance = CalculateCost(OutputList);
		}

		private static List<List<Int16>> NextGeneration()
		{
			List<List<Int16>> newPopulation = new List<List<Int16>>(PopulationSize);
			//Random rand1 = new Random();
			//Random rand2 = new Random();
			ChooseElites(newPopulation);

			for (int i = 0; i < (PopulationSize - ElitismSize) / 2; i++)
			{
				List<Int16> parentA = TournamentSelection();
				List<Int16> parentB = TournamentSelection();

				Tuple<List<Int16>, List<Int16>> children = CrossoverOX(parentA, parentB);
				List<Int16> child1 = children.Item1;
				List<Int16> child2 = children.Item2;

				if (rand1.NextDouble() < MutationRate)
					MutationInvert(child1);
				if (rand2.NextDouble() < MutationRate)
					MutationInvert(child2);
				newPopulation.Add(child1);
				newPopulation.Add(child2);
			}

			return newPopulation;
		}

		private static List<Int16> TournamentSelection()
		{
			//Random rand = new Random();
			int index = rand1.Next(PopulationSize);
			int bestIndex = index;
			double bestFitness = fitnessList[index];

			for (int i = 1; i < TournamentSize; i++)
			{
				index = rand1.Next(PopulationSize);
				if (fitnessList[index] > bestFitness)
				{
					//best = population[index];
					bestFitness = fitnessList[index];
					bestIndex = index;
				}
			}

			List<Int16> best = population[bestIndex];

			return best;
		}

		private static int FindFittest(List<double> fitnessListPar)
		{
			double max = 0;
			int maxIndex = 0;

			for (int j = 0; j < fitnessListPar.Count; j++)
			{
				if (fitnessListPar[j] > max)
				{
					max = fitnessListPar[j];
					maxIndex = j;
				}
			}

			return maxIndex;
		}

		private static void ChooseElites(List<List<Int16>> newGeneration)
		{
			List<List<Int16>> oldGeneration = new List<List<Int16>>(population);
			List<double> oldFitnessList = new List<double>(fitnessList);
			int maxIndex;

			for (int i = 0; i < ElitismSize; i++)
			{
				/*max = 0;
				maxIndex = 0;
				for (int j = 0; j < PopulationSize; j++)
				{
					if (oldFitnessList[j] > max)
					{
						max = oldFitnessList[j];
						maxIndex = j;
					}
				}*/
				maxIndex = FindFittest(oldFitnessList);

				newGeneration.Add(oldGeneration[maxIndex]);
				oldGeneration.RemoveAt(maxIndex);
				oldFitnessList.RemoveAt(maxIndex);
			}
		}

		private static Tuple<List<Int16>, List<Int16>> CrossoverPMX(List<Int16> parentA, List<Int16> parentB)
		{
			//Random rand1 = new Random();
			//Random rand2 = new Random();
			int startIndex = rand1.Next(1, parentA.Count - 2);
			int endIndex = rand2.Next(startIndex + 1, parentA.Count - 1);
			List<Int16> child1 = new List<Int16>(parentA.Count);
			List<Int16> child2 = new List<Int16>(parentA.Count);

			for (int i = 0; i < child1.Capacity; i++)
			{
				child1.Add(0);
				child2.Add(0);
			}

			for (int i = startIndex; i <= endIndex; i++)
			{
				child1[i] = parentA[i];
				child2[i] = parentB[i];
			}

			int itA = 1, itB = 1;

			for (int i = 1; i < startIndex; i++)
			{
				for (; itB < parentB.Count - 1; itB++)
				{
					if (!child1.Contains(parentB[itB]))
					{
						child1[i] = parentB[itB];
						break;
					}
				}
				for (; itA < parentA.Count - 1; itA++)
				{
					if (!child2.Contains(parentA[itA]))
					{
						child2[i] = parentA[itA];
						break;
					}
				}
			}

			for (int i = endIndex + 1; i < child1.Capacity - 1; i++)
			{
				for (; itB < parentB.Count - 1; itB++)
				{
					if (!child1.Contains(parentB[itB]))
					{
						child1[i] = parentB[itB];
						break;
					}
				}
				for (; itA < parentA.Count - 1; itA++)
				{
					if (!child2.Contains(parentA[itA]))
					{
						child2[i] = parentA[itA];
						break;
					}
				}
			}

			Tuple<List<Int16>, List<Int16>> returnTuple = new Tuple<List<Int16>, List<Int16>>(child1, child2);

			return returnTuple;
		}

		private static Tuple<List<Int16>, List<Int16>> CrossoverOX(List<Int16> parentA, List<Int16> parentB)
		{
			//Random rand1 = new Random();
			//Random rand2 = new Random();
			int startIndex = rand1.Next(1, parentA.Count - 2);
			int endIndex = rand2.Next(startIndex + 1, parentA.Count - 1);
			List<Int16> child1 = new List<Int16>(parentA.Count);
			List<Int16> child2 = new List<Int16>(parentA.Count);

			for (int i = 0; i < child1.Capacity; i++)
			{
				child1.Add(0);
				child2.Add(0);
			}

			for (int i = startIndex; i <= endIndex; i++)
			{
				child1[i] = parentA[i];
				child2[i] = parentB[i];
			}

			int itA = 1, itB = 1;

			for (int i = endIndex + 1; i < child1.Capacity - 1; i++)
			{
				for (; itB < parentB.Count - 1; itB++)
				{
					if (!child1.Contains(parentB[itB]))
					{
						child1[i] = parentB[itB];
						break;
					}
				}
				for (; itA < parentA.Count - 1; itA++)
				{
					if (!child2.Contains(parentA[itA]))
					{
						child2[i] = parentA[itA];
						break;
					}
				}
			}

			for (int i = 1; i < startIndex; i++)
			{
				for (; itB < parentB.Count - 1; itB++)
				{
					if (!child1.Contains(parentB[itB]))
					{
						child1[i] = parentB[itB];
						break;
					}
				}
				for (; itA < parentA.Count - 1; itA++)
				{
					if (!child2.Contains(parentA[itA]))
					{
						child2[i] = parentA[itA];
						break;
					}
				}
			}

			Tuple<List<Int16>, List<Int16>> returnTuple = new Tuple<List<Int16>, List<Int16>>(child1, child2);

			return returnTuple;
		}

		private static Tuple<List<Int16>, List<Int16>> Crossover(List<Int16> parentA, List<Int16> parentB)
		{
			//Random rand1 = new Random();
			//Random rand2 = new Random();
			int startIndex = rand1.Next(1, parentA.Count - 2);
			int endIndex = rand2.Next(startIndex + 1, parentA.Count - 1);
			List<Int16> child1 = new List<Int16>(parentA.Count);
			List<Int16> child2 = new List<Int16>(parentA.Count);

			//for (int i = 0; i < child1.Capacity; i++)
			//{
			child1.Add(0);
			child2.Add(0);
			//}

			for (int i = startIndex; i <= endIndex; i++)
			{
				child1.Add(parentA[i]);
				child2.Add(parentB[i]);
			}

			for (int i = 1; i < parentA.Count - 1; i++)
			{
				if (!child1.Contains(parentA[i]))
					child1.Add(parentA[i]);
			}
			for (int i = 1; i < parentB.Count - 1; i++)
			{
				if (!child2.Contains(parentB[i]))
					child2.Add(parentB[i]);
			}
			child1.Add(0);
			child2.Add(0);

			Tuple<List<Int16>, List<Int16>> returnTuple = new Tuple<List<Int16>, List<Int16>>(child1, child2);

			return returnTuple;
		}

		private static void MutationSwap(List<Int16> sol)
		{
			//Random rand = new Random();
			int point1 = rand1.Next(1, sol.Count - 1);
			int point2 = rand1.Next(1, sol.Count - 1);

			while (point1 == point2)
				point2 = rand1.Next(1, sol.Count - 1);

			Swap(point1, point2, sol);
		}

		private static void MutationInvert(List<Int16> sol)
		{
			//Random rand = new Random();
			int startIndex = rand1.Next(1, sol.Count - 2);
			int endIndex = rand1.Next(startIndex + 1, sol.Count - 1);
			int howMany = endIndex - startIndex;

			sol.Reverse(startIndex, howMany);
		}

		private static int CalculateCost(List<Int16> sol)
		{
			int temp = 0;
			Int16 i1, i2;

			i1 = sol[0];

			for (int i = 1; i < sol.Count; i++)
			{
				i2 = sol[i];
				temp += InputGraph[i1][i2];
				i1 = i2;
			}

			return temp;
		}

		private static double CalculateFitness(List<Int16> sol)
		{
			int distance = CalculateCost(sol);

			return 10000.0 / distance;
		}

		private static void Swap(int i, int j, List<Int16> sol)
		{
			Int16 temp = sol[i];
			sol[i] = sol[j];
			sol[j] = temp;
		}

		private static void RandomizeSolution(List<Int16> sol, int seed)
		{
			Random rand = new Random(seed);
			for (int i = 1; i < sol.Count - 1; i++)
			{
				int j = rand.Next(1, sol.Count - 1);
				Swap(i, j, sol);
			}
		}

		private static List<Int16> GenerateIndividual(List<Int16> sol)
		{
			List<Int16> toReturn = new List<Int16>(sol);
			Random rand = new Random();
			for (int i = 1; i < toReturn.Count - 1; i++)
			{
				int j = rand.Next(1, toReturn.Count - 1);
				Swap(i, j, toReturn);
			}

			return toReturn;
		}

		public static void ShowResults()
		{
			if (OutputList != null)
			{
				Console.WriteLine("Algorytm genetyczny - wyniki");
				Console.WriteLine("Parametry:");
				Console.WriteLine("Wielkość populacji: " + PopulationSize);
				Console.WriteLine("Ilość pokoleń: " + Generations);
				Console.WriteLine("Wielkość puli turniejowej: " + TournamentSize);
				Console.WriteLine("Ilość elit: " + ElitismSize);
				Console.WriteLine("Szansa na mutację: " + MutationRate);
				Console.WriteLine("Długość ścieżki: " + PathDistance);
				Console.WriteLine("Kolejność miast: ");
				foreach (var city in OutputList)
				{
					Console.Write(city + " ");
				}
				Console.Write('\n');
				Console.WriteLine("Wynik wyznaczono w czasie: " + TimeMeasured.TotalMilliseconds + " ms.");
			}
		}
	}//class
}//namespace