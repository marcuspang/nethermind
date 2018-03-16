﻿/*
 * Copyright (c) 2018 Demerzel Solutions Limited
 * This file is part of the Nethermind library.
 *
 * The Nethermind library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * The Nethermind library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Numerics;
using System.Security.Cryptography;

namespace Nethermind.Core.Extensions
{
    public static class BigIntegerExtensions
    {
        private static readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();
        
        public static BigInteger Abs(this BigInteger @this)
        {
            return BigInteger.Abs(@this);
        }

        public static byte[] ToBigEndianByteArray(this BigInteger bigInteger, int outputLength = -1)
        {
            byte[] fromBigInteger = bigInteger.ToByteArray();
            int trailingZeros = fromBigInteger.TrailingZerosCount();
            if (fromBigInteger.Length == trailingZeros)
            {
                return new byte[outputLength == -1 ? 1 : outputLength];
            }

            byte[] result = new byte[fromBigInteger.Length - trailingZeros];
            for (int i = 0; i < result.Length; i++)
            {
                result[fromBigInteger.Length - trailingZeros - 1 - i] = fromBigInteger[i];
            }

            if (bigInteger.Sign < 0 && outputLength != -1)
            {
                byte[] newResult = new byte[outputLength];
                Buffer.BlockCopy(result, 0, newResult, outputLength - result.Length, result.Length);
                for (int i = 0; i < outputLength - result.Length; i++)
                {
                    newResult[i] = 0xff;
                }

                return newResult;
            }

            if (outputLength != -1)
            {
                return result.PadLeft(outputLength);
            }

            return result;
        }

        // TODO: review, replace with optimal algorithm
        public static BigInteger ModInverse(this BigInteger a, BigInteger n)
        {
            BigInteger i = n, v = 0, d = 1;
            while (a > 0)
            {
                BigInteger t = i / a, x = a;
                a = i % x;
                i = x;
                x = d;
                d = v - t * x;
                v = x;
            }

            v %= n;
            if (v < 0)
            {
                v = (v + n) % n;
            }

            return v;
        }

        public static int BitLength(this BigInteger a)
        {
            int bitLength = 0;

            while (a / 2 != 0)
            {
                a /= 2;
                bitLength++;
            }

            return bitLength + 1;
        }

        public static bool TestBit(this BigInteger a, int i)
        {
            return (a & (BigInteger.One << i)) != 0;
        }
        
        // TODO: review this implementation / avoid in PROD code
        public static bool IsProbablePrime(this BigInteger source, int certainty)
        {   
            if (source == 2 || source == 3)
            {
                return true;
            }

            if (source < 2 || source % 2 == 0)
            {
                return false;
            }

            BigInteger d = source - 1;
            int s = 0;

            while (d % 2 == 0)
            {
                d /= 2;
                s += 1;
            }

            // There is no built-in method for generating random BigInteger values.
            // Instead, random BigIntegers are constructed from randomly generated
            // byte arrays of the same length as the source.
            byte[] bytes = new byte[source.ToByteArray().LongLength];
            BigInteger a;

            for (int i = 0; i < certainty; i++)
            {
                do
                {
                    // This may raise an exception in Mono 2.10.8 and earlier.
                    // http://bugzilla.xamarin.com/show_bug.cgi?id=2761
                    Random.GetBytes(bytes);
                    a = new BigInteger(bytes);
                } while (a < 2 || a >= source - 2);

                BigInteger x = BigInteger.ModPow(a, d, source);
                if (x == 1 || x == source - 1)
                {
                    continue;
                }

                for (int r = 1; r < s; r++)
                {
                    x = BigInteger.ModPow(x, 2, source);
                    if (x == 1)
                    {
                        return false;
                    }

                    if (x == source - 1)
                    {
                        break;
                    }
                }

                if (x != source - 1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}