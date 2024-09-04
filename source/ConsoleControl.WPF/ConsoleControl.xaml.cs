//https://github.com/dwmkerr/consolecontrol/blob/master/source/ConsoleControl.WPF/ConsoleControl.xaml.cs
using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ConsoleControlAPI;

namespace ConsoleControl.WPF
{
    /// <summary>
    /// Interaction logic for ConsoleControl.xaml
    /// </summary>
    public partial class ConsoleControl : UserControl
    {
        /// <summary>
        ///   Implements a set of predefined colors.
        ///   Thank you https://stackoverflow.com/a/28211440/3757184
        /// </summary>
        public class ConsoleColors
        {
            /// <summary>
            ///   The system-defined color for ConsoleColor.DarkGreen (#008000).
            /// </summary>
            private static Color _commandOutput = Color.FromRgb(0x00, 0x80, 0x00);

            /// <summary>
            ///   The system-defined color for ConsoleColor.DarkRed (#800000).
            /// </summary>
            private static Color _systemError = Color.FromRgb(0x80, 0x00, 0x00);

            /// <summary>
            ///   The system-defined color for ConsoleColor.DarkCyan (#008080).
            /// </summary>
            private static Color _systemInput = Color.FromRgb(0x00, 0x80, 0x80);

            /// <summary>
            ///   The system-defined color for ConsoleColor.Gray (#C0C0C0).
            /// </summary>
            private static Color _systemOutput = Color.FromRgb(0xC0, 0xC0, 0xC0);

            /// <summary>
            ///   Gets the system-defined color for ConsoleColor.DarkGreen (#008000).
            /// </summary>
            /// 
            /// <returns>Represents colors in terms of alpha, red, green, and blue channels.</returns>
            public static Color CommandOutput
            {
                get { return _commandOutput; }
            }

            /// <summary>
            ///   Gets the system-defined color for ConsoleColor.DarkRed (#800000).
            /// </summary>
            /// 
            /// <returns>Represents colors in terms of alpha, red, green, and blue channels.</returns>
            public static Color SystemError
            {
                get { return _systemError; }
            }

            /// <summary>
            ///   Gets the system-defined color for ConsoleColor.DarkCyan (#008080).
            /// </summary>
            public static Color SystemInput
            {
                get { return _systemInput; }
            }

            /// <summary>
            ///   Gets the system-defined color for ConsoleColor.Gray (#C0C0C0).
            /// </summary>
            public static Color SystemOutput
            {
                get { return _systemOutput; }
            }
        }

        /// <summary>
         /// Occurs when console input is produced.
         /// </summary>
        public event ProcessEventHandler ProcessInput;

        /// <summary>
        /// Current position that input starts at.
        /// </summary>
        private int inputStartPos;

        /// <summary>
        /// The internal process interface used to interface with the process.
        /// </summary>
        private ProcessInterface _processInterface;

        /// <summary>
        /// Gets or sets a value indicating whether this instance has input enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has input enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsInputEnabled
        {
            get { return (bool)GetValue(IsInputEnabledProperty); }
            set { SetValue(IsInputEnabledProperty, value); }
        }

        private static readonly DependencyProperty IsInputEnabledProperty =
          DependencyProperty.Register("IsInputEnabled", typeof(bool), typeof(ConsoleControl),
          new PropertyMetadata(true));

        internal static readonly DependencyPropertyKey IsProcessRunningPropertyKey =
          DependencyProperty.RegisterReadOnly("IsProcessRunning", typeof(bool), typeof(ConsoleControl),
          new PropertyMetadata(false));

        private static readonly DependencyProperty IsProcessRunningProperty = IsProcessRunningPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether this instance has a process running.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has a process running; otherwise, <c>false</c>.
        /// </value>
        public bool IsProcessRunning
        {
            get { return (bool)GetValue(IsProcessRunningProperty); }
            private set { SetValue(IsProcessRunningPropertyKey, value); }
        }

        private static readonly DependencyProperty ProcessInterfaceProperty =
          DependencyProperty.Register("ProcessInterface", typeof(ProcessInterface), typeof(ConsoleControl),
          new PropertyMetadata(null, new PropertyChangedCallback(OnProcessInterfaceChanged)));

        /// <summary>
        /// Gets the internally used process interface.
        /// </summary>
        /// <value>
        /// The process interface.
        /// </value>
        public ProcessInterface ProcessInterface
        {
            get { return (ProcessInterface)GetValue(ProcessInterfaceProperty); }
            set { SetValue(ProcessInterfaceProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show diagnostics.
        /// </summary>
        /// <value>
        ///   <c>true</c> if show diagnostics; otherwise, <c>false</c>.
        /// </value>
        public bool ShowDiagnostics
        {
            get => (bool)GetValue(ShowDiagnosticsProperty);
            set => SetValue(ShowDiagnosticsProperty, value);
        }

        private static readonly DependencyProperty ShowDiagnosticsProperty =
          DependencyProperty.Register("ShowDiagnostics", typeof(bool), typeof(ConsoleControl),
          new PropertyMetadata(false, OnShowDiagnosticsChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleControl"/> class.
        /// </summary>
        public ConsoleControl()
        {
            InitializeComponent();

            //  Wait for key down messages on the rich text box.
            richTextBoxConsole.PreviewKeyDown += richTextBoxConsole_PreviewKeyDown;
        }

        /// <summary>
        /// Handles the OnProcessError event of the processInterace control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ProcessEventArgs"/> instance containing the event data.</param>
        void ProcessInterface_ProcessError(object sender, ProcessEventArgs args)
        {
            //  Write the output, in red
            WriteOutput(args.Content, ConsoleColors.SystemError);
        }

        /// <summary>
        /// Handles the OnProcessOutput event of the processInterace control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ProcessEventArgs"/> instance containing the event data.</param>
        void ProcessInterface_ProcessEvent(object sender, ProcessEventArgs args)
        {
            switch (args.ProcessType)
            {
                case ProcessType.Start:
                    break;
                case ProcessType.StartOutput:
                case ProcessType.CommandPrompt:
                    ProcessInterface_ProcessOutput(sender, args);
                    break;
                case ProcessType.Input:
                    ProcessInterface_ProcessInput(sender, args);
                    break;
                case ProcessType.CommandOutput:
                    ProcessInterface_ProcessCommandOutput(sender, args);
                    break;
                case ProcessType.Error:
                    ProcessInterface_ProcessError(sender, args);
                    break;
                case ProcessType.Exit:
                    ProcessInterface_ProcessExit(sender, args);
                    break;
            }
        }

        void ProcessInterface_ProcessOutput(object sender, ProcessEventArgs args)
        {
            //  Write the output, in white
            WriteOutput(args.Content, ConsoleColors.SystemOutput);
        }

        void ProcessInterface_ProcessCommandOutput(object sender, ProcessEventArgs args)
        {
            //  Write the output, in green
            WriteOutput(args.Content, ConsoleColors.CommandOutput);
        }

        /// <summary>
        /// Handles the OnProcessInput event of the processInterace control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ProcessEventArgs"/> instance containing the event data.</param>
        void ProcessInterface_ProcessInput(object sender, ProcessEventArgs args)
        {
            //  Write the output, in cyan
            WriteOutput(args.Content, ConsoleColors.SystemInput);

            OnProcessInput(args);
        }

        /// <summary>
        /// Handles the OnProcessExit event of the processInterace control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ProcessEventArgs"/> instance containing the event data.</param>
        void ProcessInterface_ProcessExit(object sender, ProcessEventArgs args)
        {
            //  Read only again.
            RunOnUIDispatcher(() =>
            {
                //  Are we showing diagnostics?
                if (ShowDiagnostics)
                {
                    WriteOutput(Environment.NewLine + _processInterface.ProcessFileName + " exited.", Color.FromArgb(255, 0, 255, 0));
                }

                //richTextBoxConsole.IsReadOnly = true;
                IsInputEnabled = false;

                //  And we're no longer running.
                IsProcessRunning = false;
            });
        }

        private static void OnProcessInterfaceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Handle process events. Note handler is static. Need to call an instance based method to subscribe.
            // https://stackoverflow.com/a/2453249/3757184
            ConsoleControl consoleControl = (ConsoleControl)d;
            consoleControl.SubscribeProcessEvent();
        }

        /// <summary>
        /// Fires the console input event.
        /// </summary>
        /// <param name="args">The <see cref="ProcessEventArgs"/> instance containing the event data.</param>
        private void OnProcessInput(ProcessEventArgs args)
        {
            ProcessInput?.Invoke(this, args);
        }

        private static void OnShowDiagnosticsChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
        }

        /// <summary>
        /// Handles the KeyDown event of the richTextBoxConsole control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs" /> instance containing the event data.</param>
        void richTextBoxConsole_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var caretPosition = richTextBoxConsole.GetCaretPosition();
            var delta = caretPosition - inputStartPos;
            var inReadOnlyZone = delta < 0;

            //  If we're at the input point and it's backspace, bail.
            if (inReadOnlyZone && e.Key == Key.Back)
                e.Handled = true;

            //  Are we in the read-only zone?
            if (inReadOnlyZone)
            {
                //  Allow arrows and Ctrl-C.
                if (!(e.Key == Key.Left ||
                    e.Key == Key.Right ||
                    e.Key == Key.Up ||
                    e.Key == Key.Down ||
                    (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))))
                {
                    e.Handled = true;
                }
            }

            //  Is it the return key?
            if (e.Key == Key.Return)
            {
                //  Get the input.
                var input = new TextRange(richTextBoxConsole.GetPointerAt(inputStartPos), richTextBoxConsole.Selection.Start).Text;

                //  Write the input (without echoing).
                WriteInput(input, Colors.White, false);
            }
        }

        /// <summary>
        /// Clears the output.
        /// </summary>
        public void ClearOutput()
        {
            richTextBoxConsole.Document.Blocks.Clear();
            inputStartPos = 0;
        }

        /// <summary>
        /// Writes the input to the console control.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="color">The color.</param>
        /// <param name="echo">if set to <c>true</c> echo the input.</param>
        public void WriteInput(string input, Color color, bool echo)
        {
            RunOnUIDispatcher(() =>
            {
                Debug.WriteLine($"WriteInput ({Environment.CurrentManagedThreadId}): {input}");
                //  Are we echoing?
                if (echo)
                {
                    richTextBoxConsole.Selection.ApplyPropertyValue(TextBlock.ForegroundProperty, new SolidColorBrush(color));
                    richTextBoxConsole.AppendText(input);
                    inputStartPos = richTextBoxConsole.GetEndPosition();
                }

                //  Write the input.
                _processInterface.WriteInput(input);

                //  Fire the event.
                OnProcessInput(new ProcessEventArgs(ProcessType.Input, input));
            });
        }

        /// <summary>
        /// Writes the output to the console control.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="color">The color.</param>
        public void WriteOutput(string output, Color color)
        {
            RunOnUIDispatcher(() =>
            {
                Debug.WriteLine($"WriteOutput ({Environment.CurrentManagedThreadId}): {output}");
                //  Write the output.
                var range = new TextRange(richTextBoxConsole.GetEndPointer(), richTextBoxConsole.GetEndPointer())
                {
                    Text = output
                };
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));

                //  Record the new input start.
                richTextBoxConsole.ScrollToEnd();
                richTextBoxConsole.SetCaretToEnd();
                inputStartPos = richTextBoxConsole.GetCaretPosition();
            });
        }

        /// <summary>
        /// Runs the on UI dispatcher.
        /// </summary>
        /// <param name="action">The action.</param>
        private void RunOnUIDispatcher(Action action)
        {
            if (Dispatcher.CheckAccess())
            {
                //  Invoke the action.
                action();
            }
            else
            {
                Dispatcher.BeginInvoke(action, null);
            }
        }

        /// <summary>
        /// Runs a process.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="arguments">The arguments.</param>
        public void StartProcess(string fileName, string arguments)
        {
            StartProcess(new ProcessStartInfo(fileName, arguments));
        }

        /// <summary>
        /// Runs a process.
        /// </summary>
        /// <param name="processStartInfo"><see cref="ProcessStartInfo"/> to pass to the process.</param>
        public void StartProcess(ProcessStartInfo processStartInfo)
        {
            //  Are we showing diagnostics?
            if (ShowDiagnostics)
            {
                WriteOutput("Preparing to run " + processStartInfo.FileName, Color.FromArgb(255, 0, 255, 0));
                if (!string.IsNullOrEmpty(processStartInfo.Arguments))
                    WriteOutput(" with arguments " + processStartInfo.Arguments + "." + Environment.NewLine, Color.FromArgb(255, 0, 255, 0));
                else
                    WriteOutput("." + Environment.NewLine, Color.FromArgb(255, 0, 255, 0));
            }

            //  Start the process.
            _processInterface.StartProcess(processStartInfo);

            RunOnUIDispatcher(() =>
            {
                //  If we enable input, make the control not read only.
                //if (IsInputEnabled)
                //    richTextBoxConsole.IsReadOnly = false;

                //  We're now running.
                IsProcessRunning = true;
                    
            });
        }

        /// <summary>
        /// Stops the process.
        /// </summary>
        public void StopProcess()
        {
            //  Stop the interface.
            _processInterface.StopProcess();
        }

        // Instance method to subsribe to ProcessEvent from PropertyChangedCallback. Seemed cleaner than doing it in ConsoleControl_Loaded event handler.
        // https://stackoverflow.com/a/2453249/3757184
        private void SubscribeProcessEvent()
        {
            ProcessInterface.ProcessEvent += ProcessInterface_ProcessEvent;
        }
    }
}
