﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Rudine.Util
{
    /// <summary>
    ///     A Base36 De- and Encoder
    /// </summary>
    internal static class Base36
    {
        private const string CharList = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>
        ///     Encode the given number into a Base36 string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Encode(long input)
        {
            if (input < 0)
                throw new ArgumentOutOfRangeException("input", input, "input cannot be negative");

            char[] clistarr = CharList.ToCharArray();
            Stack<char> result = new Stack<char>();
            while (input != 0)
            {
                result.Push(clistarr[input % 36]);
                input /= 36;
            }
            return new string(result.ToArray());
        }

        /// <summary>
        ///     Decode the Base36 Encoded string into a number
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static long Decode(string input)
        {
            IEnumerable<char> reversed = input.ToLower().Reverse();
            long result = 0;
            int pos = 0;
            foreach (char c in reversed)
            {
                result += CharList.IndexOf(c) * (long) Math.Pow(36, pos);
                pos++;
            }
            return result;
        }
    }
}