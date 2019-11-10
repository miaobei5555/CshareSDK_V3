using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeYun.Api
{
    /// <summary>
    /// 更新信息
    /// </summary>
    public class UpdateInfo
    {
        /// <summary>
        /// 更新描述
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// 当前版本主程序md5
        /// </summary>
        public string Md5 { get; set; }

        /// <summary>
        /// 版本名
        /// </summary>
        public string VerName { get; set; }

        /// <summary>
        /// 这里是下载地址
        /// </summary>
        public string Url { get; set; }

        public UpdateInfo(string desc, string md5, string verName, string url)
        {
            this.Desc = desc;
            this.Md5 = md5;
            this.VerName = verName;
            this.Url = url;
        }

    }
}
