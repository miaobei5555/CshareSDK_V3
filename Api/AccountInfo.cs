using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeYun.Api
{
    /// <summary>
    /// 账户信息
    /// </summary>
    public class AccountInfo
    {
        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// QQ
        /// </summary>
        public string QQ { get; set; }

        /// <summary>
        /// 剩余点数
        /// </summary>
        public string Mobile { get; set; }

        /// <summary>
        /// 手机号码
        /// </summary>
        public string Point { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public string TimeOut { get; set; }

        /// <summary>
        /// 用户个人的邀请码，可用于邀请别的用户获得奖励，奖励具体在软件管理->注册/赠送 设置
        /// </summary>
        public string Code { get; set; }

        public AccountInfo(string email, string qq, string mobile, string point, string timeout, string code)
        {
            this.Email = email;
            this.QQ = qq;
            this.Mobile = mobile;
            this.Point = point;
            this.TimeOut = timeout;
            this.Code = code;
        }
    }
}
