/*! 
@author https://stackoverflow.com/questions/3655430/selection-based-on-percentage-weighting
@lastupdate 24 December 2017
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ProportionValue<T>
{
    public double Proportion { get; set; }
    public T Value { get; set; }
}

public static class ProportionValue
{
    public static ProportionValue<T> Create<T>(double proportion, T value)
    {
        return new ProportionValue<T> { Proportion = proportion, Value = value };
    }

    static System.Random random = new System.Random();
    public static T ChooseByRandom<T>(
        this IEnumerable<ProportionValue<T>> collection)
    {
		double total = 0;

        foreach (var item in collection)
        {
            total += item.Proportion;
        }

		foreach (var item in collection)
			item.Proportion = item.Proportion / total;
		
        var rnd = random.NextDouble();

        foreach (var item in collection)
        {
            if (rnd < item.Proportion)
                return item.Value;
            rnd -= item.Proportion;
        }
        throw new InvalidOperationException(
            "The proportions in the collection do not add up to 1.");
    }
}