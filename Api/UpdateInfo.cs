using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeYun.Api
{
    /// <summary>
    /// 更新信息
    /// </summary>
    [Serializable]
    public class UpdateInfo
    {
            /// <summary>
            /// 要启动的软件名称 如果文件名称 没有. 可以不需要加.exe
            /// </summary>
            public string AppName { get; set; }

            /// <summary>
            /// 应用程序版本
            /// </summary>
            public string AppVersion { get; set; }

            /// <summary>
            /// 升级需要的最低版本 暂时不需要用到
            /// </summary>
            public string RequiredMinVersion { get; set; }

            public string MD5 { get; set; }

            /// <summary>
            /// 更新描述
            /// </summary>

            public string Desc { get; set; }

            /// <summary>
            /// 下载url
            /// </summary>
            public string DownUrl { get; set; }


    }
}
