using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using Microsoft.Xrm.Sdk;

namespace Utilities
{
    class Utilities
    {
        public static DifferenceCollection getStates(Entity preImage, Entity postImage)
        {
            DifferenceCollection col = new DifferenceCollection();

            //For any Message that has a Pre and/or Post Image. I will add the values to the Audit Differences. 
            foreach (var prop in preImage.Attributes)
            {
                if (prop.Key != String.Concat(preImage.LogicalName, "id") && prop.Key != "modifiedby" && prop.Key != "modifiedon" && prop.Key != "createdon")
                {
                    col.Add(new Difference(prop.Key, Utilities.GetPropertyValue(prop), ""));
                }
            }

            foreach (var prop in postImage.Attributes)
            {
                if (prop.Key != String.Concat(postImage.LogicalName, "id") && prop.Key != "modifiedby" && prop.Key != "modifiedon" && prop.Key != "createdby" && prop.Key != "createdon")
                {
                    if (col.Contains(prop.Key))
                    {
                        Difference diff = col[prop.Key];
                        diff.CurrentValue = Utilities.GetPropertyValue(prop);
                        col[prop.Key] = diff;
                    }
                    else
                    {
                        col.Add(new Difference(prop.Key, "", Utilities.GetPropertyValue(prop)));
                    }
                }
            }

            return col;
        }

        public static DifferenceCollection getDifferences(Entity preImage, Entity postImage)
        {
            DifferenceCollection state = getStates(preImage, postImage);
            DifferenceCollection differences = new DifferenceCollection();

            foreach (Difference diff in state)
            {
                if (diff.PreviousValue != diff.CurrentValue)
                {
                    differences.Add(diff);
                }
            }

            return differences;
        }

        public static string GetPropertyValue(object p)
        {
            try
            {
                if (p.ToString() == "Microsoft.Xrm.Sdk.EntityReference")
                {
                    return ((EntityReference)p).Name;
                }
                if (p.ToString() == "Microsoft.Xrm.Sdk.OptionSetValue")
                {
                    return ((OptionSetValue)p).Value.ToString();
                }
                if (p.ToString() == "Microsoft.Xrm.Sdk.Money")
                {
                    return ((Money)p).Value.ToString();
                }
                if (p.ToString() == "Microsoft.Xrm.Sdk.AliasedValue")
                {
                    return ((AliasedValue)p).Value.ToString();
                }
                else
                {
                    return p.ToString();
                }
            }
            catch
            {
                return ""; 
            }
        }
    }

    public class DifferenceCollection : CollectionBase
    {
        private Hashtable attributes = new Hashtable();

        public DifferenceCollection()
        {
        }

        public DifferenceCollection(DifferenceCollection coll)
        {
            this.InnerList.AddRange(coll);
        }

        public Difference this[int index]
        {
            get { return (Difference)List[index]; }
            set { List[index] = value; attributes.Add(value.AttributeName, index); }
        }

        public Difference this[string attributeName]
        {
            get { return (Difference)List[(int)attributes[attributeName]]; }
            set { List[(int)attributes[attributeName]] = value; }
        }

        public virtual void Add(Difference difference)
        {
            List.Add(difference);
            attributes.Add(difference.AttributeName, attributes.Count);
        }

        public virtual void Remove(Difference difference)
        {
            List.Remove(difference);
            attributes.Remove(difference.AttributeName);
        }

        public bool Contains(Difference difference)
        {
            return List.Contains(difference);
        }

        public bool Contains(string attributeName)
        {
            return attributes.Contains(attributeName);
        }

        public int IndexOf(Difference difference)
        {
            return List.IndexOf(difference);
        }
    }

    public class Difference
    {
        private string _attributename;
        private string _previousvalue;
        private string _currentvalue;

        public Difference()
        {
        }

        public Difference(string attributename, string previousvalue, string currentvalue)
        {
            _attributename = attributename;
            _previousvalue = previousvalue;
            _currentvalue = currentvalue;
        }

        public string AttributeName
        {
            get { return _attributename; }
            set { _attributename = value; }
        }

        public string PreviousValue
        {
            get { return _previousvalue; }
            set { _previousvalue = value; }
        }

        public string CurrentValue
        {
            get { return _currentvalue; }
            set { _currentvalue = value; }
        }
    }
}
