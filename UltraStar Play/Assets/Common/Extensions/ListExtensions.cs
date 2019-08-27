using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions
{
    // Returns the elements of the list that are before the given element.
    // Thereby, the given element is included in the result list if inclusive is true.
    // If the given element is not in the list, then an empty list is returned.
    public static List<T> ElementsBefore<T>(this List<T> list, T element, bool inclusive) {
        int indexOfElement = list.IndexOf(element);
        if(indexOfElement < 0) {
            return new List<T>();
        } else {
            var result = list.GetRange(0, (inclusive) ? indexOfElement + 1 : indexOfElement);
            return result;
        }
    }

    // Returns the element in the list that comes after the given element.
    // If the given element is not in the list, then null is returned.
    public static T ElementAfter<T>(this List<T> list, T element) {
        int indexOfElement = list.IndexOf(element);
        if(indexOfElement >= 0 && list.Count > indexOfElement + 1) {
            return list[indexOfElement + 1];
        } else {
            return default(T);
        }
    }
}
