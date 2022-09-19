using System;
using System.Runtime.Serialization;

namespace weatherd
{
    /// <inheritdoc />
    /// <summary>
    /// Represents an error for insufficient meteorlogical information to calculate a solution.
    /// </summary>
    /// <remarks>Generally utilized when details such as temperature or dewpoint are missing and the class is
    /// incapable of extrapolating the data.</remarks>
    [Serializable]
    public class InsufficientMeteorologicalInformationException : Exception
    {
        /// <inheritdoc />
        /// <summary>
        /// Creates an <see cref="T:MeteorologyTester.InsufficientWXInformationException" />.
        /// </summary>
        public InsufficientMeteorologicalInformationException()
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates an <see cref="T:MeteorologyTester.InsufficientWXInformationException" /> with a message.
        /// </summary>
        /// <param name="message">Message to show.</param>
        public InsufficientMeteorologicalInformationException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates an <see cref="T:MeteorologyTester.InsufficientWXInformationException" /> with a message and an underlying exception.
        /// </summary>
        /// <param name="message">Message to show.</param>
        /// <param name="inner">The underlying exception.</param>
        public InsufficientMeteorologicalInformationException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates an <see cref="T:MeteorologyTester.InsufficientWXInformationException" /> from a serialization context.
        /// </summary>
        /// <param name="info">Information for serialization</param>
        /// <param name="context">Contextual details</param>
        protected InsufficientMeteorologicalInformationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
