using System;

namespace ConsoleControlAPI
{
    public enum ProcessType
    {
        Start,  // Created from Output
        StartOutput,  // Created from Output
        Input,
        CommandOutput,  // Created from Output
        CommandPrompt,  // Created from Output
        Error,
        Exit   // Created from Output
    }

    /// <summary>
    /// The ProcessEventArgs are arguments for a console event.
    /// </summary>
    public class ProcessEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the content.
        /// </summary>
        //public bool IsCommand { get; }

        /// <summary>
        /// Gets the content.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
        public int? Code { get; }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
        public ProcessType ProcessType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessEventArgs"/> class.
        /// </summary>
        public ProcessEventArgs(ProcessType processType) : this(processType, "", 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessEventArgs"/> class.
        /// </summary>
        /// <param name="content">The content.</param>
        public ProcessEventArgs(ProcessType processType, string content) : this(processType, content, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessEventArgs"/> class.
        /// </summary>
        /// <param name="code">The code.</param>
        public ProcessEventArgs(ProcessType processType, int code) : this(processType, "", code)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessEventArgs"/> class.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="code">The code.</param>
        public ProcessEventArgs(ProcessType processType, string content, int code)
        {
            ProcessType = processType;
            Content = content;
            Code = code;
        }
    }
}