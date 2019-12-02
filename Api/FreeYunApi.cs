using System;
using System.Collections.Generic;

using System.Text;

using System.Security.Cryptography;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JinYiHelp.EasyHTTPClient;
using System.Threading.Tasks;

namespace FreeYun.Api
{
    /// <summary>
    /// 
    /// </summary>
    public static  class FreeYunApi
    {
        #region 属性/方法
        public static string LastErr = "";

      

        private static int appid = 0;

        private static string desKey = string.Empty;

        private static string secretkey = string.Empty;

        private static string salt = string.Empty;
        private static string macCode = string.Empty;
        private static string version = string.Empty;

        public static string mToken { get; set; }//登录成功后 获取
        public static string mUser { get; set; }//登录成功才获取
        /// <summary>
        /// 通过状态码匹配出提示信息
        /// </summary>
        /// <param name="code">状态码</param>
        /// <returns>提示信息</returns>
        private static string GetMsg(string code)
        {
            if (code == "")
                return "网络错误";
            var errors = Properties.Resources.freeyunError.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var error in errors)
            {
                var tmp = error.Split(new string[] { "：" }, StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length >= 2 && tmp[1].Trim() == code)
                {
                    LastErr = tmp[0].Trim();
                    return tmp[0].Trim();
                }
                    
            }

            LastErr = "未知错误:" + code;
            return "未知错误:" + code;
        }

        /// <summary>
        /// 公共请求
        /// </summary>
        /// <param name="wtype">请求操作的类型（值范围在：1-18）具体请求对应值请看对应请求对应值</param>
        /// <param name="body">提交数据</param>
        /// <returns>内容</returns>
        private async static Task<string> Request(int wtype, string body)
        {
            try
            {
                var timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now);


                var tem = FreeYunUtil.DesEncryptToByte(body, desKey, CipherMode.ECB, PaddingMode.PKCS7);
                var hex = FreeYunUtil.ByteToHex(tem);
                //var url = "https://api.freeyun.net/webgateway.html";

                var url = "https://bgp.freeyun.net/webgateway.html";

                var _tstr = string.Format("{0}{1}{2}{3}{4}", wtype, timestamp, salt, appid, hex);
                var sign = FreeYunUtil.MD5(_tstr);//version=1.1.3&
                var sendBody = "appid={0}&secretkey={1}&wtype={2}&sign={3}&timestamp={4}&data={5}";//这个版本号 是sdk网络验证的版本号 非软件的
                sendBody = string.Format(sendBody, appid, secretkey, wtype, sign, timestamp, hex);

                var item = new HttpItem()
                {
                    URL=url,
                    Method=System.Net.Http.HttpMethod.Post,
                    Postdata=sendBody,
                    Encoding=Encoding.UTF8,
                    ContentType= "application/x-www-form-urlencoded",
                    Timeout=30,
                    
                };

                item.Header.Add("Version", "1.1.3");

                var result =await item.GetHtml();

                string html = result.Html;

                /*
                 * 
                    var buffer = Encoding.UTF8.GetBytes(sendBody);
                retry:
                var req = HttpWebRequest.Create(url);
                req.Timeout = 60 * 1000;
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = buffer.Length;
                req.Headers.Add("Version", "1.1.3");

                req.GetRequestStream().Write(buffer, 0, buffer.Length);
                var html = string.Empty;
                using (var rsp = req.GetResponse())
                {
                    using (var reader = new StreamReader(rsp.GetResponseStream()))
                    {
                        html = reader.ReadToEnd();
                    }
                }
                if (html == "")
                {
                    Thread.Sleep(1000);
                    goto retry;
                }

                */



                JObject json = (JObject)JsonConvert.DeserializeObject(html);
                var msg = json["msg"].ToString();
                var data = json["data"].ToString();
                var status = json["status"].ToString();
                if (status == "-1")
                {
                    throw new Exception(FreeYunUtil.IsNull(msg) ? "未知错误" : msg);
                }
                   
             
                string str = Encoding.UTF8.GetString(FreeYunUtil.DesDecrypt(FreeYunUtil.HexToByte(data), desKey, CipherMode.ECB, PaddingMode.PKCS7));

                return str;
                

            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
                return "";
            }

            // body = "{\"version\":\"1.1.1.0\",\"timestamp\":1501150648401}";

        }


        #endregion
        /// <summary>
        /// 软件取初始化信息接口_1
        /// </summary>
        /// <param name="_appid">软件id</param>
        /// <param name="_secretkey">软件密钥</param>
        /// <param name="_salt">加密盐</param>
        /// <param name="_desKey">des 密钥</param>
        /// <param name="_mackCode">机器码</param>
        /// <param name="_version">当前版本号</param>
        /// <returns></returns>
        public async static  Task<InitInfo> Init(int _appid, string _secretkey, string _salt, string _desKey, string _mackCode, string _version)
        {
            appid = _appid;
            secretkey = _secretkey;
            salt = _salt;
            desKey = _desKey;
            macCode = _mackCode;
            version = _version;

            try
            {
                string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();

                JObject dic = new JObject();
                dic.Add("version", version);
                dic.Add("timestamp", timestamp);
                dic.Add("macCode", macCode);
                dic.Add("secretKey", secretkey);

                //string data = "{" + $"'version':'{version}','timestamp':{timestamp},'macCode':'{macCode}','secretKey':'{secretkey}'"Replace("'", "\"") + "}";
                string data = JsonConvert.SerializeObject(dic);


                var ret =await Request(1, data);

                if (string.IsNullOrEmpty(ret)) return null;

                if (!FreeYunUtil.IsJson(ret))
                {
                    LastErr = "服务器返回为空";
                    return null ;
                }

                JObject json = (JObject)JsonConvert.DeserializeObject(ret);

                if (json == null)
                {
                    
                    return null;
                }

                var code = json["code"].ToString();
                if (code != "1003")
                {
                    LastErr = GetMsg(code);
                    Console.WriteLine("初始化失败,原因:" + GetMsg(code));
                    return null;
                }


                var nowVersion = json["nowVersion"].ToString();
                var lastVersion = json["lastVersion"].ToString();
                var needUpdate = true;
                var md5 = json["md5"].ToString();
                var notic = json["notic"].ToString();
                var baseData = json["baseData"].ToString();
                return new InitInfo(nowVersion, lastVersion, needUpdate, md5, notic, baseData);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return null;
            }


        }



        /// <summary>
        /// 帐号注册_2
        /// </summary>
        /// <param name="usr">账号</param>
        /// <param name="pwd">密码</param>
        /// <param name="qq">QQ选填</param>
        /// <param name="email">邮箱选填</param>
        /// <param name="mobile">手机号码选填</param>
        /// <param name="invitingCode">邀请码选填</param>
        /// <param name="agentCode">代理商代理编号，该参数为代理商列表的代理编号，可空，可内置在软件用于指定不同的代理商注册的用户所有权</param>
        public async static  Task<Result_Info> Register(string usr, string pwd, 
            string qq="", string email="", string mobile="", string invitingCode="",string agentCode="")
        {
            string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();

            Result_Info info = new Result_Info();

            JObject dic = new JObject();

            dic.Add("account", usr);
            dic.Add("password", pwd);
            dic.Add("macCode", macCode);
            dic.Add("timestamp", timestamp);
            dic.Add("secretKey", secretkey);

            if (!FreeYunUtil.IsNull(qq))
            {
                dic.Add("qq", qq);
            }


            if (!FreeYunUtil.IsNull(email))
            {
                dic.Add("email", email);
            }


            if (!FreeYunUtil.IsNull(mobile))
            {
                dic.Add("mobile", mobile);
            }


            if (!FreeYunUtil.IsNull(invitingCode))
            {
                dic.Add("mobile", mobile);
            }


            if (!FreeYunUtil.IsNull(agentCode))
            {
                dic.Add("agentCode", agentCode);
            }

            try
            {

                string data = JsonConvert.SerializeObject(dic);


                var ret =await Request(2, data);

                if (!FreeYunUtil.IsJson(ret))
                {
                    info.Html = ret;
                    return info;
                }

                JObject json = (JObject)JsonConvert.DeserializeObject(ret);
                var code = json["code"].ToString();
                if (code != "1006")
                {
                    info.Html = "注册失败,原因:" + GetMsg(code);

                }
                else
                {
                    info.Html = "注册成功";
                    info.Is_bool = true;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);

                info.Html = ex.Message;


            }

            return info;
        }

        /// <summary>
        /// 登录_3
        /// </summary>
        /// <param name="usr">账号</param>
        /// <param name="pwd">密码</param>
        /// <param name="version">软件本地版本</param>
        /// <returns>TOKEN</returns>
        public async static  Task<Result_Info> Login(string usr, string pwd)
        {
            Result_Info info = new Result_Info();
            try
            {
                string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();
                var md5 = FreeYunUtil.FileHashCode(Application.ExecutablePath);

                JObject dic = new JObject();

                dic.Add("account", usr);
                dic.Add("password", pwd);
                dic.Add("macCode", macCode);
                dic.Add("version", version);
                dic.Add("md5", md5);
                dic.Add("timestamp", timestamp);
                dic.Add("secretKey", secretkey);


                string data = JsonConvert.SerializeObject(dic);

                var ret =await Request(3, data);
                JObject json = (JObject)JsonConvert.DeserializeObject(ret);
                var code = json["code"].ToString();


                if (code != "1014")
                {
                    info.Html = "登录失败,原因:" + GetMsg(code);
                   
                }
                else
                {
                    info.Html= "登录成功";
                    info.other = json["token"].ToString();
                    mUser = usr;
                    mToken = info.other;
                    info.Is_bool = true;
                   
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                info.Html= ex.StackTrace;
               
            }

            return info;
        }

        /// <summary>
        /// 卡号充值_4
        /// </summary>
        /// <param name="usr">账号</param>
        /// <param name="cardNo">卡号</param>
        public async static  Task<Result_Info> Card(string usr,string cardNo)
        {
            var info = new Result_Info();

            try
            {

                string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();

                JObject dic = new JObject();

                dic.Add("account", usr);
                dic.Add("cardNo", cardNo);
                dic.Add("macCode", macCode);
                           
                dic.Add("timestamp", timestamp);
                dic.Add("secretKey", secretkey);

                string data = JsonConvert.SerializeObject(dic);
                    
                var ret =await Request(4, data);
                JObject json = (JObject)JsonConvert.DeserializeObject(ret);
                var code = json["code"].ToString();

                if (code != "1029")
                {
                    info.Html = "充值失败,原因:" + GetMsg(code);

                }
                else
                {
                    info.Html = "充值成功";
                    info.Is_bool = true;
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                info.Html = ex.Message;
            }

            return info;
        }

        /// <summary>
        /// 创建支付链接_5
        /// </summary>
        /// <param name="cardTypeId">充值卡类型ID，该值从取卡类型列表接口可获得</param>
        public async static  Task<Result_Info> CreatePay( string cardTypeId)
        {
            string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();

            var info = new Result_Info();

            try
            {

            JObject dic = new JObject();

            dic.Add("account", mUser);
            dic.Add("cardTypeId", cardTypeId);
            dic.Add("macCode", macCode);

            dic.Add("timestamp", timestamp);
            dic.Add("secretKey", secretkey);

            string data = JsonConvert.SerializeObject(dic);
 
            var ret =await Request(5, data);
            JObject json = (JObject)JsonConvert.DeserializeObject(ret);
            var code = json["code"].ToString();
           
           
            if (code != "1037")
            {
               
                info.Html= "创建支付失败,原因:" + GetMsg(code);
            }else
            {
                var payUrl = json["payUrl"].ToString();
                info.other = payUrl;
                info.Html = "创建支付成功";
                info.Is_bool = true;
            }

            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);

                info.Html = ex.Message;

            }

            return info;

        }


        /// <summary>
        /// 取账号信息_6
        /// </summary>
        /// <param name="usr">账号</param>
        /// <param name="token">登录返回的TOKEN</param>
        /// <param name="err_code">查询错误返回错误码</param>
        /// <returns>账号信息</returns>
        public async static  Task<AccountInfo> Info()
        {
            string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();

            JObject dic = new JObject();

            if (string.IsNullOrEmpty(mToken))
            {
                return null ;
            }

            dic.Add("account", mUser);
            dic.Add("token", mToken);
            dic.Add("macCode", macCode);

            dic.Add("timestamp", timestamp);
            dic.Add("secretKey", secretkey);

            string data = JsonConvert.SerializeObject(dic);


            //var data = "{" + $"'account':'{usr}','token':'{token}','timestamp':{timestamp},'macCode':'{macCode}','secretKey':'{secretkey}'".Replace("'", "\"") + "}";

            var ret =await Request(6, data);
            JObject json = (JObject)JsonConvert.DeserializeObject(ret);
            var code = json["code"].ToString();
            string status;
            AccountInfo ainfo;

            if (code != "1017")
            {
                status = GetMsg(code);
                ainfo = new AccountInfo("", "", "", "", "", "");
                Console.WriteLine("查用户信息失败,原因:" + GetMsg(code));
            }
            else
            {
             
                Console.WriteLine("info={0}", ret);
                var email = json["email"].ToString();
                var qq = json["qq"].ToString();
                var mobile = json["mobile"].ToString();
                var point = json["point"].ToString();
                var timeout = json["timeout"].ToString();
                var invitingcode = json["invitingcode"].ToString();
                ainfo = new AccountInfo(email, qq, mobile, point, timeout, invitingcode);
                status = "ok";
            }

            return ainfo;
        }


        /// <summary>
        /// 加入黑名单_7 加入黑名单 有误 待排查
        /// </summary>
        /// <param name="usr">账号</param>
        /// <param name="blackType">/黑名单类型：1、IP黑名单  2、机器码黑名单</param>
        public async static  Task<Result_Info> Diss(string usr, int blackType=1)
        {
            string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();

            Result_Info info = new Result_Info();

            try
            {
                JObject dic = new JObject();

              
                dic.Add("account", usr);
               
                dic.Add("macCode", macCode);

                dic.Add("timestamp", timestamp);
                dic.Add("secretKey", secretkey);
                dic.Add("cardTypeId", blackType);

                string data = JsonConvert.SerializeObject(dic);

                //var data = "{" + $"'account':'{usr}','cardTypeId':'{blackType}','timestamp':{timestamp},'macCode':'{macCode}','secretKey':'{secretkey}'".Replace("'", "\"") + "}";
          
            var ret =await Request(20, data);
            JObject json = (JObject)JsonConvert.DeserializeObject(ret);
            var code = json["code"].ToString();

            if (code != "1048")
            {
                info.Html="失败,原因:" + GetMsg(code);
            }
            else
            {

                    info.Html = "ok";
                    info.Is_bool = true;
            }


            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                info.Html = ex.Message;
            }

            return info;

        }


        /// <summary>
        /// 修改密码_8
        /// </summary>
        /// <param name="usr">账号</param>
        /// <param name="oldPwd">旧密码</param>
        /// <param name="newPwd">新密码</param>
        public async static  Task<Result_Info> ChangePwd(string usr, string oldPwd, string newPwd)
        {

            string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();

            var info = new Result_Info();
            try
            {


            JObject dic = new JObject();

            dic.Add("account", usr);
            dic.Add("oldPwd", oldPwd);
            dic.Add("newPwd", newPwd);
            dic.Add("macCode", macCode);

            dic.Add("timestamp", timestamp);
            dic.Add("secretKey", secretkey);

            string data = JsonConvert.SerializeObject(dic);

            //var data = "{" + $"'account':'{usr}','oldPwd':'{oldPwd}','newPwd':'{newPwd}','timestamp':{timestamp},'macCode':'{macCode}','secretKey':'{secretkey}'".Replace("'", "\"") + "}";

            var ret =await Request(13, data);
            JObject json = (JObject)JsonConvert.DeserializeObject(ret);
            var code = json["code"].ToString();
            if (code != "1026")
            {
                    info.Html="修改密码失败,原因:" + GetMsg(code);
            }
            else
            {
                    info.Html = "修改成功";
                    info.Is_bool = true;
            }

            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                info.Html = ex.Message;
            }


            return info;
        }



        /// <summary>
        /// 登出
        /// </summary>
        /// <param name="usr">账号</param>
        /// <param name="token">退出的token</param>
        /// <returns>是否退出成功</returns>
        public async static Task<bool> Logout()
        {
            string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();

            JObject dic = new JObject();

            dic.Add("account", mUser);
            dic.Add("token", mToken);
        
            dic.Add("macCode", macCode);

            dic.Add("timestamp", timestamp);
            dic.Add("secretKey", secretkey);

            string data = JsonConvert.SerializeObject(dic);

           // var data = "{" + $"'account':'{usr}','token':'{token}','timestamp':{timestamp},'macCode':'{macCode}','secretKey':'{secretkey}'".Replace("'", "\"") + "}";

            var ret =await Request(14, data);
            JObject json = (JObject)JsonConvert.DeserializeObject(ret);
            if (json == null) return false;

            var code = json["code"].ToString();
            return code == "1";
        }

        /// <summary>
        /// 取更新信息
        /// </summary>
        /// <returns>更新信息或Null</returns>
        public async static Task<UpdateInfo> GetUpdateInfo()
        {
            string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();

            JObject dic = new JObject();

            dic.Add("version", version);
          
            dic.Add("macCode", macCode);

            dic.Add("timestamp", timestamp);
            dic.Add("secretKey", secretkey);

            string data = JsonConvert.SerializeObject(dic);

            //var data = "{" + $"'version':'{version}','timestamp':{timestamp},'macCode':'{macCode}','secretKey':'{secretkey}'".Replace("'", "\"") + "}";

            var ret =await Request(15, data);

            if (!FreeYunUtil.IsJson(ret))
            {
                throw new Exception("更新 json 格式 不正确");
            }

            Console.WriteLine(ret);
            JObject json = (JObject)JsonConvert.DeserializeObject(ret);
            var code = json["code"].ToString();
            if (code != "1033" && code != "1002")
                throw new Exception("取更新信息失败,原因:" + GetMsg(code));
            if (code == "1002")
                return null;
            var describe = json["describe"].ToString();
            var md5 = json["md5"].ToString();
            var name = json["name"].ToString();
            var url = json["url"].ToString();

            var upINfo = new UpdateInfo();
            upINfo.Desc = describe;
            upINfo.MD5 = md5;
            upINfo.DownUrl = url;
            upINfo.AppName = name;

            return upINfo;

        }

        //===============以下功能 未测试 有bug 自行改下=========================
        /// <summary>
        /// 读取充值卡类型列表
        /// </summary>
        /// <returns>充值卡类型列表</returns>
        public async static Task<List<CardType>> CardTypeList()
        {
            string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();

            JObject dic = new JObject();
            dic.Add("timestamp", timestamp);
            dic.Add("macCode", macCode);
            dic.Add("secretKey", secretkey);
           

            var cardTypeList = new List<CardType>();
            var ret =await Request(16, "");
            JObject json = (JObject)JsonConvert.DeserializeObject(ret);
            var code = json["code"].ToString();

            if (code != "1035")
                throw new Exception("读取卡类型失败,原因:" + GetMsg(code));

            var cardList = FreeYunUtil.ToJArray((json["cardList"].ToString()));
            if (cardList != null && cardList.Count != 0)
            {
                for (var i = 0; i < cardList.Count; i++)
                {
                    var str = cardList[i].ToString();
                    JObject json_temp = (JObject)JsonConvert.DeserializeObject(str);
                    var price = FreeYunUtil.ToInt(json_temp["price"].ToString()) / 100;
                    var name = json_temp["name"].ToString();
                    var id = json_temp["id"].ToString();
                    var value = FreeYunUtil.ToInt(json_temp["value"].ToString());
                    cardTypeList.Add(new CardType(name, id, price, value));
                }
            }
            return cardTypeList;
        }



        /// <summary>
        /// (登录后)取远程变量值
        /// </summary>
        /// <param name="usr">账号</param>
        /// <param name="token">登录返回的TOKEN</param>
        /// <param name="keyName">变量名</param>
        /// <returns>变量值</returns>
        public async static Task< Result_Info> ReadVariable (string keyName)
        {
            string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();

            var info = new Result_Info();

            if (string.IsNullOrEmpty(mToken))
            {
                info.Html = "token不能为空";
                return info;
            }

            try { 

            JObject dic = new JObject();
            dic.Add("account",mUser);
            dic.Add("token", mToken);
            dic.Add("keyName", keyName);
            dic.Add("timestamp", timestamp);
            dic.Add("macCode", macCode);
            dic.Add("secretKey", secretkey);

            string data = JsonConvert.SerializeObject(dic);

            var ret =await Request(8, data);
            JObject json = (JObject)JsonConvert.DeserializeObject(ret);
            var code = json["code"].ToString();


            if (code != "1019")
            {
                info.Html= "读变量失败,原因:" + GetMsg(code);
            }
            else
            {
                var variable = json["variable"].ToString();
                info.Html = "ok";
                info.other = variable;
                info.Is_bool = true;
            }

            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            return info;
        }




        /// <summary>
        /// (登录后)心跳[发送频率建议为：10-20分钟一次]
        /// </summary>
        /// <param name="usr">账号</param>
        /// <param name="token">登录返回的TOKEN</param>
        public async static Task< Result_Info> Heartbeat()
        {
            string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();

            var info = new Result_Info();

            var data = "{" + $"'account':'{mUser}','token':'{mToken}','timestamp':{timestamp},'macCode':'{macCode}','secretKey':'{secretkey}'".Replace("'", "\"") + "}";
            try
            {

            var ret =await Request(9, data);

                if (!FreeYunUtil.IsJson(ret))
                {
                    info.Html = "网络错误";
                }

            JObject json = (JObject)JsonConvert.DeserializeObject(ret);
            var code = json["code"].ToString();
           
            if (code != "1046")
            {
                info.Html = GetMsg(code);

            }else
             {
                info.Html ="ok";
                info.Is_bool = true;
            }

            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                info.Html = ex.Message;
            }

            return info;
        }



        /// <summary>
        /// 换绑
        /// </summary>
        /// <param name="usr">账号</param>
        /// <param name="pwd">密码</param>
        /// <param name="macCode">新的机器码</param>
        /// <param name="userType">账号类型，2、单码类型</param>
        public async static void ModifyMac(string usr, string pwd, string _macCode, int userType = 1)
        {
            string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();
            var data = "{" + $"'account':'{usr}','userType':'{userType}','password':'{pwd}','timestamp':{timestamp},'macCode':'{_macCode}','secretKey':'{secretkey}'".Replace("'", "\"") + "}";

            var ret =await Request(10, data);
            JObject json = (JObject)JsonConvert.DeserializeObject(ret);
            var code = json["code"].ToString();
            if (code != "1032")
                throw new Exception("换绑失败,原因:" + GetMsg(code));
        }



        /// <summary>
        /// 取账号状态 是否到期 等
        /// </summary>
        /// <param name="usr">账号</param>
        /// <param name="token">登录返回的TOKEN</param>
        /// <returns>账号信息</returns>
        public async static Task< Result_Info> InfoStatus()
        {
            string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();
            var data = "{" + $"'account':'{mUser}','token':'{mToken}','timestamp':{timestamp},'macCode':'{macCode}','secretKey':'{secretkey}'".Replace("'", "\"") + "}";

            var info = new Result_Info();

            try
            {

                var ret =await Request(17, data);
                JObject json = (JObject)JsonConvert.DeserializeObject(ret);
                var code = json["code"].ToString();
                string status;


                if (code != "1039")
                {
                    status = GetMsg(code);
                    info.Html = "取用户状态失败,原因: " + GetMsg(code);
                
                }
                else
                {
                    info.Html = "ok";
                    info.Is_bool = true;
                }

                }catch(Exception ex)
                {
                    info.Html = ex.Message;

                }
                return info;
        }



       /// <summary>
       /// 远程算法转发
       /// </summary>
       /// <param name="remoteId"> 转发列表对应id</param>
       /// <param name="_params"></param>
       /// <returns></returns>
        public async static Task<Result_Info> TranspondSer(string remoteId,string _params)
        {
            string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();
            JObject dic = new JObject();
            dic.Add("account",mUser);
            dic.Add("token",mToken);
            dic.Add("timestamp", timestamp);
            dic.Add("macCode",macCode);
            dic.Add("secretKey", secretkey);
            dic.Add("remoteId", remoteId);
            dic.Add("params",_params);

            var data = JsonConvert.SerializeObject(dic);


            var info = new Result_Info();

            try
            {


                var ret =await Request(22, data);
                JObject json = (JObject)JsonConvert.DeserializeObject(ret);
                var code = json["code"].ToString();
                string status;


                if (code != "1051")
                {
                    status = GetMsg(code);
                    info.Html = "远程转发失败,原因: " + GetMsg(code);

                }
                else
                {
                    info.Html = "ok";
                    info.other = json["result"].ToString();
                    info.Is_bool = true;
                }

            }
            catch (Exception ex)
            {
                info.Html = ex.Message;

            }
            return info;
        }



        /// <summary>
        /// 取在线人数
        /// </summary>
        /// <param name="remoteId"> 转发列表对应id</param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public async static Task< Result_Info> GetOnLineCount()
        {
            string timestamp = FreeYunUtil.ToTimeStamp(DateTime.Now).ToString();
            JObject dic = new JObject();
            dic.Add("version", version);
         
            dic.Add("timestamp", timestamp);
            dic.Add("macCode", macCode);
            dic.Add("secretKey", secretkey);
          

            var data = JsonConvert.SerializeObject(dic);


            var info = new Result_Info();

            try
            {


                var ret =await Request(23, data);
                JObject json = (JObject)JsonConvert.DeserializeObject(ret);
                var code = json["code"].ToString();
                string status;


                if (code != "1054")
                {
                    status = GetMsg(code);
                    info.Html = "取在线人数失败,原因: " + GetMsg(code);

                }
                else
                {
                    info.Html = "ok";
                    info.other = json["onlineNum"].ToString();
                    info.Is_bool = true;
                }

            }
            catch (Exception ex)
            {
                info.Html = ex.Message;

            }
            return info;
        }

        //==========以前的分割线=========








    }
}
