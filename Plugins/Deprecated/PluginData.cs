using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Layouter.Plugins
{
    public class PluginData
    {
        public PluginData()
        {

        }

        public PluginData(string caption, string data)
        {
            Caption = caption;
            Data = data;
        }

        public string Caption { get; set; }

        public string Data { get; set; }
    }

    public class PluginComplexData : PluginData
    {
        public PluginComplexData()
        { 

        }

        public PluginComplexData(string caption, string data) : base(caption, data)
        {
        }

        public PluginComplexData(string caption, string data, string iconPath, object tag) : this(caption, data)
        {
            IconPath = iconPath;
            Tag = tag;
        }

        public PluginComplexData(string caption, string data, string iconPath) : this(caption, data, iconPath, null)
        {
        }

        public string IconPath { get; set; }

        public object Tag { get; set; } = null;
    }


    public class PluginDataList<T> where T : PluginData
    {

        public PluginDataList()
        {
            Items = new List<T>();
        }

        public List<T> Items { get; set; }


        public PluginDataList<T> Add(T item)
        {
            Items.Add(item);
            return this;
        }

        public PluginDataList<T> Add(params T[] items)
        {
            Items.AddRange(items);
            return this;
        }

        public PluginDataList<T> Remove(T item)
        {
            Items.Remove(item);
            return this;
        }

        public PluginDataList<T> Remove(int index, int count)
        {
            Items.RemoveRange(index, count);
            return this;
        }

        public PluginDataList<T> Clear()
        {
            Items.Clear();
            return this;
        }


        public void Solve()
        {

        }



    }
}
