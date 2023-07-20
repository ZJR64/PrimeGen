///CSCI 251 Project 2
///
/// This program finds large prime numbers. It takes a bit size greater than 32 and divisible by 8, plus a number of primes to generate. The program then finds
/// the required number of specified sized prime numbers and displays them to the user along with the time it took to process. This program uses parallel processing
/// to complete the large amount of processing required to find such large prime numbers and uses the number of proccessors available to it to determine the
/// number of threads to use, so as to minimize wasted resources while maximizing work done. Naturally computers with less processors will perform worse than those
/// that have more.
/// 
///@author Zak Rutherford Email:zjr6302@rit.edu
///Created Oct 2022
///
using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Security.Cryptography;
using System.Numerics;

namespace CSCI251Project1
{
    /// <summary>
    /// Class used to implement extension methods for BigIntegers.
    /// </summary>
    static class BigIntExtension
    {
        /// <summary>
        /// Extension method for BigIntegers. Assumes BigInteger is positive and odd. Determines if the BigInteger is prime or not
        /// by using the Miller-Rabin Primality Test. The higher k is, the higher the rate of acccuracy.
        /// </summary>
        /// <param name="value"> The BigInteger being tested as prime.</param>
        /// <param name="k"> Default is 10. The number of iterations the algorithm will use, the higher the more accurate, but it takes more time.</param>
        /// <returns>True if probably prime, false if composite.</returns>
        public static Boolean isProbablyPrime(this BigInteger value, int k = 10)
        {
            //find d
            var d = value - 1;
            var s = 0;
            while (d % 2 == 0)
            {
                s++;
                d = d / 2;
            }

            //create RandomNumberGenerator instance
            RandomNumberGenerator rng = RandomNumberGenerator.Create();

            //do loop k times
            for (int i = 0; i < k; i++)
            {
                //get a
                BigInteger a = 0;
                while (a < 2 || a > value - 2)
                {
                    var byteArray = new byte[value.ToByteArray().Length];
                    rng.GetBytes(byteArray);
                    a = new BigInteger(byteArray);
                }
                //get x
                var x = BigInteger.ModPow(a, d, value);
                if (x == 1 || x == value - 1)
                {
                    continue;
                }
                //repeat s - 1 times
                for (int j = 0; j < s - 1; j++)
                {
                    x = BigInteger.ModPow(x, 2, value);
                    if (x == value - 1)
                    {
                        continue;
                    }
                }
                //composite
                return false;
            }
            //is probably prime
            return true;
        }
    }

    /// <summary>
    /// Class used to find large prime numbers.
    /// </summary>
    public class PrimeGen
    {
        /// <summary>
        /// The Main Function. Gets the command line arguments and vets them, then when arguments are known to be good, calls parallelPrime.
        /// </summary>
        /// <param name="args"> The command line arguments</param>
        public static void Main(string[] args)
        {
            //help message for user
            var helpMessage = "Usage:  dotnet run <bits> <count=1>\n" +
                              "Create a specified number of bit sized prime numbers\n\n" +
                              "\t- bits - the number of bits of the prime number, this must be a\n" +
                              "\t  multiple of 8, and at least 32 bits.\n" +
                              "\t- count - the number of prime numbers to generate, defaults to 1";

            //check for command line arguments
            if (args.Length > 2 || args.Length < 1)
            {
                Console.WriteLine("Error: incorrect number of command line arguments.");
                Console.WriteLine(helpMessage);
                return;
            }

            int bits = 0;
            int count = 1;

            //check that both arguments are numbers
            if (!int.TryParse(args[0], out bits))
            {
                Console.WriteLine("Error: bits must be a number.");
                Console.WriteLine(helpMessage);
                return;
            }
            if (args.Length == 2)
            {
                if (!int.TryParse(args[1], out count))
                {
                    Console.WriteLine("Error: count must be a number.");
                    Console.WriteLine(helpMessage);
                    return;
                }
            }

            //check if numbers are correctly formatted
            if (bits < 32 || !(bits % 8 == 0))
            {
                Console.WriteLine("Error: bits must be greater than 32 and a muliple of 8.");
                Console.WriteLine(helpMessage);
                return;
            }
            if (count < 1)
            {
                Console.WriteLine("Error: count must be greater than 0.");
                Console.WriteLine(helpMessage);
                return;
            }
            parallelPrime(bits, count);
        }

        /// <summary>
        /// Finds the specified number of primes of specified bit size. First, the time is recorded to begin counting the time. Second, the list of small primes is created.
        /// This allows the threads to waste less time in the Miller-Rabin test. Then it creates a set amount of threads based on the number of processors available, each
        /// running findPrime. Finally, it prints the final time after allowing the threads to join.
        /// </summary>
        /// <param name="bits"> The desired size in bits of the prime number(s). </param>
        /// <param name="count"> The amount of prime numbers desired. </param>
        public static void parallelPrime(int bits, int count)
        {
            List<Thread> threads = new List<Thread>();
            List<int> primeList = new List<int>();
            int[] param = new int[3];
            DateTime[] time = new DateTime[2];
            param[0] = bits;
            param[1] = count;
            param[2] = 0;

            Console.WriteLine("BitLength: " + bits + " bits");
            //start time
            time[0] = DateTime.Now;

            //get number of proccessors to optimise number of threads.
            var threadCount = Environment.ProcessorCount;

            //create list of first (bit * 2) primes
            primeList.Add(2);
            var primeCount = (bits * 2);
            var current = 3;
            primeList.Add(2);
            var prime = false;
            while (primeCount > 0)
            {
                prime = true;
                Parallel.ForEach(primeList, num =>
                {
                    if (current % num == 0)
                    {
                        prime = false;
                    }
                });
                if (prime)
                {
                    primeList.Add(current);
                    primeCount--;
                }
                current += 2;
            }

            //create threads
            for (int i = 0; i < threadCount; i++)
            {
                var newThread = new Thread(() => findPrime(param, time, primeList));
                newThread.Start();
                threads.Add(newThread);
            }
            
            //wait for threads to join
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            Console.WriteLine("Time to Generate: " + (time[1] - time[0]));
        }

        /// <summary>
        /// Randomly creates BigIntegers of specified size until the desired number of primes is reached. 
        /// </summary>
        /// <param name="param"> The list used to pass parameters that all threads can access and modify. param[0] should be bit size of number,
        /// param[1] should be current number of primes found (0 initially), and param[2] should be the desired number of primes.</param>
        /// <param name="time"> The list used to return the finished time. time[0] should be start time, time[1] will be end time. </param>
        /// <param name="primeList"> The list of small prime numbers to mod the random numbers by, should be bigger depending on bit size. </param>
        public static void findPrime(int[] param, DateTime[] time, List<int>primeList)
        {
            //create RandomNumberGenerator instance
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            var byteArray = new byte[param[0] / 8];
            BigInteger rand = 0;
            while (param[1] > param[2])
            {
                var notReady = true;
                //create random number
                while (notReady)
                {
                    notReady = false;
                    rng.GetBytes(byteArray);
                    rand = new BigInteger(byteArray);

                    //turn positive
                    if (rand < 0)
                    {
                        rand = rand * -1;
                    }
                    foreach (int prime in primeList)
                    {
                        if (rand % prime == 0)
                        {
                            notReady = true;
                            break;
                        }
                    }
                }
                
                //if prime print and update param[1]
                if (rand.isProbablyPrime())
                {
                    lock (param)
                    {
                        if (param[1] > param[2])
                        {
                            param[2]++;
                            Console.WriteLine(param[2] + ": " + rand);

                            //create a new line if not the last number generated
                            //else get the end time
                            if (param[1] != param[2])
                            {
                                Console.WriteLine();
                            }
                            else
                            {
                                time[1] = DateTime.Now;
                            }
                        }
                    }
                }
            }
        }
    }
}