using System;
using System.Runtime.Serialization;

namespace BackupFolderAzureDurableFunctionDemo.Exceptions
{
    /// <summary>
    /// Base exception for custom exceptions.
    /// </summary>
    [Serializable]
    public class ExceptionBase : ApplicationException
    {
        /// <summary>
        /// Calls the default exception constructor.
        /// </summary>
        public ExceptionBase()
        {
        }

        /// <summary>
        /// Calls the default exception constructor with a message parameter.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public ExceptionBase(string message)
            : base(message)
        {
        }

	    /// <summary>
	    /// Calls the default exception constructor with a exception parameter.
	    /// </summary>
		/// <param name="innerException">The inner exception.</param>
		public ExceptionBase(Exception innerException)
			: base(null, innerException)
		{
		}

        /// <summary>
        /// Calls the default exception constructor with a message and innerException parameter.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ExceptionBase(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Calls the default exception constructor with a serialization info and streaming context parameter.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        public ExceptionBase(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
