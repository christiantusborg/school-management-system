namespace QuVian.SharedLibrary.Basics.Extensions.ArrayExtensions;

public static class ArrayGetCircularNextExtensions
{
    /// <summary>
    /// Retrieves the next element in the array in a circular manner.
    /// If the current index is the last element, it returns the first element.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="currentIndex">The index of the current element.</param>
    /// <returns>The next element in the array, wrapping around if necessary.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the array is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the array is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if currentIndex is out of valid range.</exception>
    public static T GetCircularNext<T>(this T[] array, int currentIndex)
    {
        // Validate that the array is not null.
        if (array is null)
            throw new ArgumentNullException(nameof(array), "The array cannot be null.");

        // Ensure that the array is not empty.
        if (array.Length == 0)
            throw new InvalidOperationException("The array must contain at least one element.");

        // Ensure the current index is within a valid range.
        if (currentIndex < 0 || currentIndex >= array.Length)
            throw new ArgumentOutOfRangeException(
                nameof(currentIndex), 
                $"The index {currentIndex} is out of range. It must be between 0 and {array.Length - 1}."
            );

        // Calculate the next index in a circular manner.
        var nextIndex = (currentIndex + 1) % array.Length;

        // Return the next element.
        return array[nextIndex];
    }
}