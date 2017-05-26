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

namespace g910_akcm
{
    public static class Extend
    {
        public static void getRGBFromHex(this string hex, out int r, out int g, out int b)
        {
            int c;
            int.TryParse(hex.Replace("#", ""), System.Globalization.NumberStyles.HexNumber | System.Globalization.NumberStyles.AllowHexSpecifier, null, out c);
            b = c >> 00 & 0xff;
            g = c >> 08 & 0xff;
            r = c >> 16 & 0xff;
        }

        public static T getDataFromJsonObject<T>(this IDictionary<string, Object> jsonObject, string key)
        {
            Object o;
            jsonObject.TryGetValue(key, out o);
            return o == null ? default(T) : (T)o;
        }
    }
}
