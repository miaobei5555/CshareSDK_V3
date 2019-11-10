using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeYun.Api
{
    /// <summary>
    /// 充值卡类型结构
    /// </summary>
    public class CardType
    {
        public string Name { get; set; }

        public string Id { get; set; }

        public int Price { get; set; }

        public int Value { get; set; }

        public CardType(string name, string id, int price, int value)
        {
            this.Name = name;
            this.Id = id;
            this.Price = price;
            this.Value = value;
        }
    }
}
