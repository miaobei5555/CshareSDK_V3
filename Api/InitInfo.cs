using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeYun.Api
{
    /// <summary>
    /// 初始化信息
    /// </summary>
    public class InitInfo
    {
        /// <summary>
        /// 当前版本号
        /// </summary>
        public string NowVersion { get; set; }

        /// <summary>
        /// 最新版本号
        /// </summary>
        public string LastVersion { get; set; }

        /// <summary>
        /// 最新版本是否强制更新，该参数只在最新版本>当前版本号去判断
        /// </summary>
        public bool NeedUpdate { get; set; }

        /// <summary>
        /// 当前版本软件主程序的md5
        /// </summary>
        public string Md5 { get; set; }

        /// <summary>
        /// 软件的公告
        /// </summary>
        public string Notic { get; set; }

        /// <summary>
        /// 软件的基础数据
        /// </summary>
        public string BaseData { get; set; }

        public InitInfo(string nowVersion, string lastVersion, bool needUpdate, string md5, string notic, string baseData)
        {
            this.NowVersion = nowVersion;
            this.LastVersion = lastVersion;
            this.NeedUpdate = needUpdate;
            this.Md5 = md5;
            this.Notic = notic;
            this.BaseData = baseData;
        }

    }
}
