using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace TestXmlGridControl
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            BasicPropertyList bag;//= new BasicPropertyList();
            /***************************************************************
             * 
             * XML文件内容示例：
             * <?xml version="1.0" encoding="utf-8"?>
             * <root>
             *  <A title="显示标题" comment="注释内容" value="结点值"/>
             *  <B title="父结点" comment="父结点示例">
             *    <C title="子结点C" comment="子结点C注释" value="c"/>
             *    <D title="子结点D" comment="子结点D注释" value="d"/>
             *  </B>             
             * </root>
             * 
             * 
             */

            XmlToPropertyList xpl = new XmlToPropertyList("test.xml");
            try
            {
                bag=xpl.ReadXml();
                //bag["LineNumberMin"] = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取XML时出错：" + ex.Message, "错误"
                    , MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new Form { Controls = { new PropertyGrid { Dock = DockStyle.Fill, SelectedObject = bag } } });
        }
    }

    /// <summary>
    /// 将XML映射为属性集
    /// </summary>
    public class XmlToPropertyList
    {
        private string xmlFile;

        private BasicPropertyList pl = new BasicPropertyList();

        public XmlToPropertyList(string xmlFile)
        {
            this.xmlFile = xmlFile;
        }

        /// <summary>
        /// 读取XML结点转换为属性集
        /// </summary>
        /// <returns></returns>
        public BasicPropertyList ReadXml()
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(this.xmlFile);
                XmlNode node;
                XmlElement el = doc.DocumentElement;

                //根结点，可以根据需要，设置是否加载
                if (el != null && el.NodeType != XmlNodeType.Comment)
                {
                    node = (XmlNode)el;
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        this.Recurse(child);
                    }
                    
                    return pl;

                }
                else
                    return null;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("异常错误：" + ex.Message);
                throw ex;
            }           
        }

        string comment = "";

        /// <summary>
        /// 递归搜索所有结点
        /// </summary>
        /// <param name="node"></param>
        private void Recurse(XmlNode node)
        {
            if (node.NodeType == XmlNodeType.Comment)
            {
                comment = node.Value;
                return;
            }
            else
            {
                if (!node.HasChildNodes)
                {
                    MetaProp mp = this.ToMetaProper(node, null);
                    pl.Properties.Add(mp);
                    pl[node.Name] = node.Attributes["value"].Value;
                    return;
                }
                else
                {
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        this.Recurse(child);
                    }
                }
            }
        }

        /// <summary>
        /// 将结点转换为属性
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private MetaProp ToMetaProper(XmlNode node,Type type,string comment="")
        {
            if (type == null) type = typeof(string);//默认使用字符串类型
            string name, title, category="", desc="";
            name = node.Name;// +"_"+DateTime.Now.Ticks; //避免名称相同，加上ticks
            if (node.Attributes["title"] != null) title = node.Attributes["title"].Value;
            else title = name;

            if (node.Attributes["comment"] != null) desc = node.Attributes["comment"].Value;
            else desc = (comment!=""?comment:title);

            if (node.ParentNode != null)
            {
                if (node.ParentNode.Attributes["title"] != null)
                    category = node.ParentNode.Attributes["title"].Value;
                else
                    category = node.Name;
            }

            MetaProp mp = new MetaProp(name, type,node.Attributes["value"].Value
                , new CategoryAttribute(category)
                , new DescriptionAttribute(desc)
                , new DisplayNameAttribute(title));
            
            

            return mp;
        }

    }

    /// <summary>
    /// 属性描述
    /// </summary>
    public class MetaProp
    {
        public MetaProp(string name, Type type, Object value,params Attribute[] attributes)
        {
            this.Name = name;
            this.Type = type;
            if (attributes != null)
            {
                Attributes = new Attribute[attributes.Length];
                attributes.CopyTo(Attributes, 0);
            }
        }
        public string Name { get; private set; }
        public Type Type { get; private set; }
        public Attribute[] Attributes { get; private set; }
        public Object Value { get; private set; }
    }

    /// <summary>
    /// 属性集
    /// </summary>
    [TypeConverter(typeof(BasicPropertyBagConverter))]
    public class BasicPropertyList
    {

        private readonly List<MetaProp> properties = new List<MetaProp>();
        public List<MetaProp> Properties { get { return properties; } }
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public object this[string key]
        {
            get { object value; return values.TryGetValue(key, out value) ? value : null; }
            set { if (value == null) values.Remove(key); else values[key] = value; }
        }

        class BasicPropertyBagConverter : ExpandableObjectConverter
        {
            public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
            {
                PropertyDescriptor[] metaProps = (from prop in ((BasicPropertyList)value).Properties
                                                  select new PropertyBagDescriptor(prop.Name, prop.Type, prop.Attributes)).ToArray();
                return new PropertyDescriptorCollection(metaProps);
            }
        }
        class PropertyBagDescriptor : PropertyDescriptor
        {
            private readonly Type type;
            public PropertyBagDescriptor(string name, Type type, Attribute[] attributes)
                : base(name, attributes)
            {
                this.type = type;
            }
            public override Type PropertyType { get { return type; } }
            public override object GetValue(object component) { return ((BasicPropertyList)component)[Name]; }
            public override void SetValue(object component, object value) { ((BasicPropertyList)component)[Name] = (string)value; }
            public override bool ShouldSerializeValue(object component) { return GetValue(component) != null; }
            public override bool CanResetValue(object component) { return true; }
            public override void ResetValue(object component) { SetValue(component, null); }
            public override bool IsReadOnly { get { return false; } }
            public override Type ComponentType { get { return typeof(BasicPropertyList); } }
        }

    }
}