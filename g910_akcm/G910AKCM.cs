/*
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LedCSharp;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace g910_akcm
{
    public class G910AKCM
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private Thread bgw = null;
        private bool bgwRunning = false;

        private List<EFT> configs = new List<EFT>();

        public G910AKCM(string configDirectory)
        {
            if (!Directory.Exists(configDirectory))
                Directory.CreateDirectory(configDirectory);

            var files = Directory.GetFiles(configDirectory, "*.eft", SearchOption.AllDirectories);
            loadConfig(files);

            bgw = new Thread(backgroundWorker);
            bgwRunning = true;
            bgw.Start();
        }

        private void backgroundWorker()
        {
            while (!LogitechGSDK.LogiLedInit())
            {
                Console.WriteLine("Try to init logitech module");
                Thread.Sleep(1000);
            }
            Thread.Sleep(500);
            LogitechGSDK.LogiLedSetTargetDevice(LogitechGSDK.LOGI_DEVICETYPE_PERKEY_RGB);
            LogitechGSDK.LogiLedSaveCurrentLighting();

            Process p = null;
            int i = -1;
            
            EFT oldEFT = null, eft = null;
            string oldProcessName = null;

            while (bgwRunning)
            {
                Thread.Sleep(1000);

                p = getActiveWindow();
                if (p.ProcessName == oldProcessName)
                    continue;

                oldProcessName = p.ProcessName;
                Console.WriteLine("new foreground => " + oldProcessName);

                eft = null;
                for(i = configs.Count;i-- > 0;)
                {
                    if(configs[i].application_list.IndexOf(oldProcessName) >= 0)
                    {
                        eft = configs[i];
                        break;
                    }
                }

                if (eft == null)
                {
                    LogitechGSDK.LogiLedRestoreLighting();
                    continue;
                }

                if (eft == oldEFT)
                    continue;
                Console.WriteLine("Use profile => " + eft.name);
                eft.activateProfile();
            }
        }

        private Process getActiveWindow()
        {
            IntPtr hWnd = GetForegroundWindow();
            uint procId = 0;
            GetWindowThreadProcessId(hWnd, out procId);
            var proc = Process.GetProcessById((int)procId);
            return proc;
        }

        private void loadConfig(string[] files) {
            IDictionary<string, Object> jsonObject = null;
            for(int i = files.Length; i-- > 0;)
            {
                jsonObject = Json.JsonParser.FromJson(File.ReadAllText(files[i]));
                configs.Add(new EFT(jsonObject));
            }
        }

        ~G910AKCM()
        {
            LogitechGSDK.LogiLedShutdown();
            bgw = null;
        }
    }

    public class EFT
    {
        public double device_layout = 0;
        public string device_model = null;
        public string device_name = null;
        public string name = null;
        public List<EFTTransition> transitionList = null;
        public List<string> application_list = null;
        public string uid = null;

        public EFT()
        {
        }

        public EFT(IDictionary<string, Object> jsonObject)
        {
            device_layout = jsonObject.getDataFromJsonObject<double>("device_layout");
            device_model = jsonObject.getDataFromJsonObject<string>("device_model");
            device_name = jsonObject.getDataFromJsonObject<string>("device_name");
            name = jsonObject.getDataFromJsonObject<string>("name");
            uid = jsonObject.getDataFromJsonObject<string>("uid");
            application_list = jsonObject.getDataFromJsonObject<IList<Object>>("application_list").Cast<String>().ToList();
            var lst = jsonObject.getDataFromJsonObject<IList<object>>("transition_list").Cast<IDictionary<string, Object>>().ToList();
            transitionList = new List<EFTTransition>(
                EFTTransition.FromEFTTransitionList(lst)
            );
        }

        public void activateProfile()
        {
            EFTTransition t = transitionList.FirstOrDefault();
            if (t == null)
                return;
            t.activateProfile();
        }
    }

    public class EFTTransition
    {
        public int r;
        public int g;
        public int b;
        public double curve;
        public double index;
        public double length;
        public List<KeybordColor> state;

        public EFTTransition(){

        }

        public EFTTransition(IDictionary<string, Object> jsonObject)
        {
            curve = jsonObject.getDataFromJsonObject<double>("curve");
            index = jsonObject.getDataFromJsonObject<double>("index");
            length = jsonObject.getDataFromJsonObject<double>("length");
            string color = jsonObject.getDataFromJsonObject<string>("color");
            color.getRGBFromHex(out r, out g, out b);
            r = (int)(r / 255f * 100);
            g = (int)(g / 255f * 100);
            b = (int)(b / 255f * 100);
            var state = jsonObject.getDataFromJsonObject<IDictionary<string, Object>>("state");
            this.state = new List<KeybordColor>();
            this.state.AddRange(
                getKeybordColor(state.getDataFromJsonObject<IDictionary<string, Object>>("1"), 0, true)
            );
            this.state.AddRange(
                getKeybordColor(state.getDataFromJsonObject<IDictionary<string, Object>>("4"), 0xFFF0)
            );
            this.state.AddRange(
                getKeybordColor(state.getDataFromJsonObject<IDictionary<string, Object>>("10"), 0xFFFF0)
            );
        }

        private static List<KeybordColor> getKeybordColor(IDictionary<string, Object> jso, int inc = 0, bool hid = false)
        {
            List<KeybordColor> lkc = new List<g910_akcm.KeybordColor>();
            if (jso == null)
                return lkc;
            int key = 0;
            foreach(var kp in jso)
            {
                int.TryParse(kp.Key, System.Globalization.NumberStyles.HexNumber | System.Globalization.NumberStyles.AllowHexSpecifier, null, out key);
                key += inc;
                if(hid)
                    lkc.Add(new KeybordColor(key, (string)kp.Value));
                else
                    lkc.Add(new KeybordColor((keyboardNames)key, (string)kp.Value));
            }

            return lkc;
        }


        public static List<EFTTransition> FromEFTTransitionList(IList<IDictionary<string, Object>> jsonObjectLst)
        {
            return jsonObjectLst.Select(x => { return new EFTTransition(x); }).ToList();
        }

        public void activateProfile()
        {
            LogitechGSDK.LogiLedSetLighting(r, g, b);
            for(int i = state.Count; i-- > 0;)
                state[i].activateProfile();
        }
    }

    public class KeybordColor
    {
        public keyboardNames kn = 0;
        public int hid = 0;
        public int r = 0;
        public int g = 0;
        public int b = 0;
        public bool hidCode = false;

        public KeybordColor()
        {

        }

        public KeybordColor(keyboardNames kn, string color)
        {
            hidCode = false;
            this.kn = kn;
            color.getRGBFromHex(out r, out g, out b);
            r = (int)(r / 255f * 100);
            g = (int)(g / 255f * 100);
            b = (int)(b / 255f * 100);
        }

        public KeybordColor(int hid, string color)
        {
            hidCode = true;
            this.hid = hid;
            color.getRGBFromHex(out r, out g, out b);
            r = (int)(r / 255f * 100);
            g = (int)(g / 255f * 100);
            b = (int)(b / 255f * 100);
        }

        public void activateProfile()
        {
            if(hidCode)
                LogitechGSDK.LogiLedSetLightingForKeyWithHidCode(hid, r, g, b);
            else
                LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(kn, r, g, b);
        }
    }
}