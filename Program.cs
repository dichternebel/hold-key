using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using GregsStack.InputSimulatorStandard;
using GregsStack.InputSimulatorStandard.Native;

namespace HoldKey
{
    internal static class Program
    {
        private const int WaitBefore = 320;
        private const int WaitAfter = 20;

        private static string appGuid = Assembly.GetExecutingAssembly().GetCustomAttribute<GuidAttribute>().Value;

        private static InputHookHelper helper = new InputHookHelper();

        private static SoundPlayer soundPlayer = new SoundPlayer(Properties.Resources.bmw_seat_belt);

        private static bool IsAltKeyPressed { get; set; }

        private static ProgramViewModel ViewModel { get; set; }

        private static int ShiftKeyPressed
        {
            get
            {
                var shiftKeyPressed = 0;
                InputSimulator inputSimulator = new InputSimulator();

                if (inputSimulator.InputDeviceState.IsKeyDown(VirtualKeyCode.SHIFT))
                {
                    shiftKeyPressed = (int)VirtualKeyCode.SHIFT;
                }
                else if (inputSimulator.InputDeviceState.IsKeyDown(VirtualKeyCode.LSHIFT))
                {
                    shiftKeyPressed = (int)VirtualKeyCode.LSHIFT;
                }
                else if (inputSimulator.InputDeviceState.IsKeyDown(VirtualKeyCode.RSHIFT))
                {
                    shiftKeyPressed = (int)VirtualKeyCode.RSHIFT;
                }
                return shiftKeyPressed;
            }
        }

        private static int currentHoldKeyCode;
        private static int CurrentHoldKeyCode
        {
            get { return currentHoldKeyCode; }
            set
            {
                currentHoldKeyCode = value;

                if (value > 0 && ViewModel.IsSoundEnabled)
                {
                    soundPlayer.PlayLooping();
                }
                else
                {
                    soundPlayer.Stop();
                }
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
            DoStart();
        }

        /// <summary>
        /// The start method.
        /// Needed to make assembly resolving work.
        /// </summary>
        static void DoStart()
        {
            using (Mutex mutex = new Mutex(false, "Global\\" + appGuid))
            {
                // Prevent app from being run in multiple instances and open system tray instead
                if (!mutex.WaitOne(0, false))
                {
                    InputSimulator inputSimulator = new InputSimulator();
                    inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.VK_B);
                    inputSimulator.Keyboard.Sleep(WaitAfter).KeyPress(VirtualKeyCode.RETURN);
                    return;
                }

                helper.NewKeyboardMessage += Helper_NewKeyboardMessage;
                helper.NewMouseMessage += Helper_NewMouseMessage;
                helper.InstallHooks();

                ViewModel = new ProgramViewModel();
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;

                string[] args = Environment.GetCommandLineArgs();
                var soundArg = "enablesound";
                if (args.Any(x => x.ToLower().Contains(soundArg))) ViewModel.IsSoundEnabled = true;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var myForm = new Form1(ViewModel);
                myForm.FormClosing += MyForm_FormClosing;
                Application.Run(myForm);
            }
        }

        /// <summary>
        /// Fires when user toggles activity sound.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IsSoundEnabled") return;

            if (ViewModel.IsSoundEnabled && CurrentHoldKeyCode > 0)
            {
                soundPlayer.PlayLooping();
            }
            else
            {
                soundPlayer.Stop();
            }
        }

        /// <summary>
        /// Hooks to assembly resolver and tries to load assembly (.dll)
        /// from executable resources it CLR can't find it locally.
        ///
        /// Used for embedding assemblies onto executables.
        ///
        /// See: http://www.digitallycreated.net/Blog/61/combining-multiple-assemblies-into-a-single-exe-for-a-wpf-application
        /// </summary>
        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var assemblyName = new AssemblyName(args.Name);

            var path = assemblyName.Name + ".dll";
            if (!assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture))
            {
                path = $"{assemblyName.CultureInfo}\\${path}";
            }

            using (var stream = executingAssembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                    return null;

                var assemblyRawBytes = new byte[stream.Length];
                stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                return Assembly.Load(assemblyRawBytes);
            }
        }

        /// <summary>
        /// Captures mouse messages.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Helper_NewMouseMessage(object sender, NewMouseMessageEventArgs e)
        {
            //Debug.WriteLine($"{e.MessageType}, x: {e.Position.x}, y: {e.Position.y}");

            if (e.MessageType == MouseMessage.LButtonUp && IsAltKeyPressed)
            {
                CurrentHoldKeyCode = (int)MouseMessage.LButtonDown;
                InputSimulator inputSimulator = new InputSimulator();
                helper.UninstallHooks();

                Debug.WriteLine($"Mode triggered for LButtonDown");
                inputSimulator.Mouse.Sleep(WaitBefore).LeftButtonDown().Sleep(WaitAfter);

                IsAltKeyPressed = false;
                helper.InstallHooks();
            }
            else if (e.MessageType == MouseMessage.LButtonUp && CurrentHoldKeyCode == (int)MouseMessage.LButtonDown)
            {
                CurrentHoldKeyCode = 0;
                Debug.WriteLine($"Mode deactivated for LButtonDown");
            }
        }

        /// <summary>
        /// Captures keyboard messages.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Helper_NewKeyboardMessage(object sender, NewKeyboardMessageEventArgs e)
        {
            //Debug.WriteLine($"{e.MessageType}, VirtKeyCode: {e.VirtKeyCode}");

            // This hits for the combination of ALT + <key>
            if (e.MessageType == KeyboardMessage.SysKeyDown
                && e.VirtKeyCode != 162 && e.VirtKeyCode != 164 && e.VirtKeyCode != 165 // exclude ALT
                && e.VirtKeyCode != 16 && e.VirtKeyCode != 160 && e.VirtKeyCode != 161 // exclude SHIFT
                && e.VirtKeyCode != 9) // exclude TAB
            {               
                CurrentHoldKeyCode = e.VirtKeyCode;
                var currentShiftKey = 0 + ShiftKeyPressed;
                InputSimulator inputSimulator = new InputSimulator();

                helper.UninstallHooks();

                if (currentShiftKey == 0)
                {
                    inputSimulator.Keyboard.Sleep(WaitBefore).KeyDown((VirtualKeyCode)e.VirtKeyCode).Sleep(WaitAfter);
                    Debug.WriteLine($"Mode triggered for key [ {(Keys)e.VirtKeyCode} ] ");
                }
                else
                {
                    inputSimulator.Keyboard.Sleep(WaitBefore).KeyDown((VirtualKeyCode)e.VirtKeyCode).Sleep(WaitBefore).KeyDown((VirtualKeyCode)currentShiftKey).Sleep(WaitAfter);
                    Debug.WriteLine($"[SHIFT {currentShiftKey}]-mode triggered for key [ {(Keys)e.VirtKeyCode} ] ");
                }

                IsAltKeyPressed = false;
                helper.InstallHooks();
            }
            // Capture ALT key down
            else if (e.MessageType == KeyboardMessage.SysKeyDown && !IsAltKeyPressed)
            {
                Debug.WriteLine($"Alt key pressed with key [ {(Keys)e.VirtKeyCode} ] ");
                IsAltKeyPressed = true;
            }
            // Capture ALT key up
            else if (( e.MessageType == KeyboardMessage.KeyUp && IsAltKeyPressed && (e.VirtKeyCode == 164 || e.VirtKeyCode == 162 || e.VirtKeyCode == 165))
                    || (e.MessageType == KeyboardMessage.SysKeyUp && IsAltKeyPressed))
            {
                Debug.WriteLine($"Alt key released with key [ {(Keys)e.VirtKeyCode} ] ");
                IsAltKeyPressed = false;
            }
            // Capture other key up
            else if (e.MessageType == KeyboardMessage.KeyUp && e.VirtKeyCode == CurrentHoldKeyCode)
            {
                CurrentHoldKeyCode = 0;
                InputSimulator inputSimulator = new InputSimulator();
                helper.UninstallHooks();
                inputSimulator.Keyboard.KeyUp((VirtualKeyCode)ShiftKeyPressed).Sleep(WaitAfter);
                helper.InstallHooks();
                Debug.WriteLine($"Mode deactivated by [ {(Keys)e.VirtKeyCode} ] key");
            }
        }

        /// <summary>
        /// Cleans up the mess on closing event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MyForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            helper.UninstallHooks();
            helper.Dispose();

            if (CurrentHoldKeyCode > 0)
            {
                soundPlayer.Stop();
                soundPlayer.Dispose();

                InputSimulator inputSimulator = new InputSimulator();
                if (CurrentHoldKeyCode == (int)MouseMessage.LButtonDown)
                {
                    inputSimulator.Mouse.LeftButtonUp();
                }
                else
                {
                    inputSimulator.Keyboard.KeyUp((VirtualKeyCode)CurrentHoldKeyCode);
                    if (ShiftKeyPressed > 0) inputSimulator.Keyboard.KeyUp((VirtualKeyCode)ShiftKeyPressed);
                }
            }
        }
    }
}