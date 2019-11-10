using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace FreeYun.Api
{
    /// <summary>
    /// 一些公共方法
    /// </summary>
    public class FreeYunUtil
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "CreateFile")]
        public static extern int CreateFile(string str, int int1, int int2, int int3, int int4, int int5, int int6);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "DeviceIoControl")]
        public static extern bool DeviceIoControl(int int1, int int2, int int3, int int4, int int5, int int6, ref int int7, int int8);

        /// <summary>
        /// 该函数返回指向缓冲区的指针
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "lstrcpyn")]
        public static extern int lstrcpyn_bytes2pointer(byte[] lpString1, byte[] lpString2, int iMaxLength);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "CloseHandle")]
        public static extern int CloseHandle(int hwnd);


        //取CPU编号 
        private static String GetCpuID()
        {
            try
            {
                var mc = new ManagementClass("Win32_Processor");
                ManagementObjectCollection moc = mc.GetInstances();

                String strCpuID = null;
                foreach (ManagementObject mo in moc)
                {
                    strCpuID = mo.Properties["ProcessorId"].Value.ToString();
                    break;
                }
                return strCpuID;
            }
            catch
            {
                return "";
            }

        }//end method 

        //取第一块硬盘编号 
        private static String GetHardDiskID()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");
                String strHardDiskID = null;
                foreach (ManagementObject mo in searcher.Get())
                {
                    strHardDiskID = mo["SerialNumber"].ToString().Trim();
                    break;
                }
                return strHardDiskID;
            }
            catch
            {
                return "";
            }
        }//end  

        /// <summary>
        /// 取指针
        /// </summary>
        private static int W2P(byte[] pcszSource)
        {
            if (pcszSource != null && pcszSource.Length > 0)
                return lstrcpyn_bytes2pointer(pcszSource, pcszSource, 0);
            return 0;
        }

        /// <summary>
        /// Ansi编码转Unicode
        /// </summary>
        private static byte[] AnsiToUnicode(string str)
        {
            byte[] bytes = Encoding.Convert(Encoding.ASCII, Encoding.Unicode, Encoding.UTF8.GetBytes(str));
            return bytes;
        }

        private static long crc32(byte[] buffer)
        {
            UInt32[] crcTable = new UInt32[256];
            var length = buffer.Length;
            if (length < 1)
                return 0;
            for (int i = 0; i < 256; i++)
            {
                UInt32 crc = (UInt32)i;
                for (int j = 8; j > 0; j--)
                {
                    if ((crc & 1) == 1)
                        crc = (crc >> 1) ^ 0xEDB88320;
                    else
                        crc >>= 1;
                }
                crcTable[i] = crc;
            }
            int iCount = buffer.Length;
            UInt32 _crc = 0xFFFFFFFF;
            for (int i = 0; i < iCount; i++)
            {
                _crc = ((_crc >> 8) & 0x00FFFFFF) ^ crcTable[(_crc ^ buffer[i]) & 0xFF];
            }
            UInt32 temp = _crc ^ 0xFFFFFFFF;
            int t = (int)temp;
            return (t);
        }

        /// <summary>
        /// MD5加密字符串
        /// </summary>
        /// <param name="input">字符串</param>
        /// <returns>md5加密结果</returns>
        public static string MD5(string input)
        {
           // input = "123456z";
            var md5 = new MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.UTF8.GetBytes(input);
            byte[] result = md5.ComputeHash(data);
            String ret = ""; ;
            for (int i = 0; i < result.Length; i++)
                ret += result[i].ToString("x2");
            return ret;
        }

        /// <summary>
        /// MD5加密byte
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <returns>md5加密结果</returns>
        public static string MD5(byte[] bytes)
        {
            var md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(bytes, 0, bytes.Length);
            StringBuilder sb = new StringBuilder();
            foreach (byte value in hash)
            {
                sb.AppendFormat("{0:x2}", value);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取机器码
        /// </summary>
        /// <returns></returns>
        public static string GetSoftCode()
        {
            var c = string.Empty;
            var driveName = @"\\.\PhysicalDrive0";
            var hPhysicalDriveIOCTL = CreateFile(driveName, 0, 3, 0, 3, 0, 0);
            if (hPhysicalDriveIOCTL == -1)
            {
                c = GetHardDiskID() + "!@#$%" + GetCpuID();
                return MD5(c).ToUpper();
            }
            var buffersize = 1024;
            var query = new byte[12];
            var buffer = new byte[buffersize];
            var cbBytesReturned = 0;
            long crc1 = 0;
            var st = DeviceIoControl(hPhysicalDriveIOCTL, 2954240, W2P(query), 12, W2P(buffer), buffersize, ref cbBytesReturned, 0);
            if (st)
            {
                crc1 = crc32(buffer);
            }
            CloseHandle(hPhysicalDriveIOCTL);
            c = crc1.ToString() + "#$@#" + GetHardDiskID() + GetCpuID();
            return MD5(c).ToUpper();
        }

        /// <summary>
        /// 读取文件MD5值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>MD5</returns>
        public static string FileHashCode(string filePath)
        {
            var cfilePath = filePath + "e";
            if (File.Exists(cfilePath))
                File.Delete(cfilePath);
            File.Copy(filePath, cfilePath);//复制一份，防止占用
            if (File.Exists(cfilePath))
            {
                var buffer = File.ReadAllBytes(cfilePath);
                System.IO.File.Delete(cfilePath);
                return MD5(buffer);
            }
            else
            {
                throw new Exception("读取文件MD5出错!");
            }
        }

        /// <summary>
        /// 将c# DateTime时间格式转换为Unix时间戳格式
        /// </summary>
        /// <param name="time">时间 DateTime.Now</param>
        /// <returns>long</returns>
        public static long ToTimeStamp(System.DateTime time)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            long t = (time.Ticks - startTime.Ticks) / 10000;            //除10000调整为13位
            return t;
        }

        public static byte[] DesEncryptToByte(string input, string key,
            CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            if (key.Length > 8)
                key = key.Substring(0, 8);
            try
            {
                using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(input);
                    des.Mode = mode;
                    des.Padding = padding;

                    des.Key = ASCIIEncoding.UTF8.GetBytes(key);
                    des.IV = ASCIIEncoding.UTF8.GetBytes(key);
                    using (var ms = new System.IO.MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(bytes, 0, bytes.Length);
                            cs.FlushFinalBlock();
                        }
                        return ms.ToArray();
                    }
                }
            }
            catch { return new byte[0]; }

        }

        public static byte[] DesDecrypt(byte[] input, string key,
            CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            if (key.Length > 8)
                key = key.Substring(0, 8);
            try
            {
                using (var des = new DESCryptoServiceProvider())
                {
                    des.Mode = mode;
                    des.Padding = padding;
                    des.Key = ASCIIEncoding.UTF8.GetBytes(key);
                    des.IV = ASCIIEncoding.UTF8.GetBytes(key);
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(input, 0, input.Length);
                            cs.FlushFinalBlock();
                            return ms.ToArray();
                        }
                    }
                }
            }
            catch { return new byte[0]; }
        }

        public static string ByteToHex(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        }

        public static byte[] HexToByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

       

        /// <summary>
        /// 将文本转换为JArray
        /// </summary>
        /// <param name="input">要转换的字符串</param>
        /// <returns>JArray或null</returns>
        public static JArray ToJArray(string input)
        {
            if (!IsJson(input))
                return null;
            JToken jtoken = JToken.Parse(input);
            if (jtoken.Type == JTokenType.Array)
                return JArray.Parse(input);
            return null;
        }

        /// <summary>
        /// 是否为json内容判断的是{}
        /// </summary>
        /// <param name="input">判断的内容</param>
        /// <returns>true是json false非json</returns>
        public static bool IsJson(string input)
        {
            if (IsNull(input))
                return false;
            input = input.Trim();
            return input.StartsWith("{") && input.EndsWith("}")
                   || input.StartsWith("[") && input.EndsWith("]");
        }

        /// <summary>
        /// 判断文本是否为null 空 或仅由空白组成,自动删除\r\n\t
        /// </summary>
        public static bool IsNull(String text)
        {
            if (text == null) return true;
            text = Regex.Replace(text, "[\r\n]|[\t]", "");
            if (string.IsNullOrWhiteSpace(text))
                return true;
            return false;
        }

        /// <summary>
        /// 文本尝试转为int
        /// </summary>
        /// <param name="input">要转换的字符串</param>
        /// <returns>0或非0</returns>
        public static int ToInt(string input)
        {
            int x = 0;
            int.TryParse(input, out x);
            return x;
        }

    }
}
