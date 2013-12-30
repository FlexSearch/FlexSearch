/**
 * Autogenerated by Thrift Compiler (0.9.1)
 *
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 *  @generated
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Thrift;
using Thrift.Collections;
//using System.ServiceModel;
using System.Runtime.Serialization;
using Thrift.Protocol;
using Thrift.Transport;

namespace FlexSearch.Api
{

  #if !SILVERLIGHT
  [Serializable]
  #endif
  [DataContract(Namespace="")]
  public partial class ShardAllocationDetail : TBase
  {

    [DataMember]
    public short ShardNumber { get; set; }

    [DataMember]
    public List<string> Nodes { get; set; }

    public ShardAllocationDetail() {
    }

    public ShardAllocationDetail(short ShardNumber, List<string> Nodes) : this() {
      this.ShardNumber = ShardNumber;
      this.Nodes = Nodes;
    }

    public void Read (TProtocol iprot)
    {
      bool isset_ShardNumber = false;
      bool isset_Nodes = false;
      TField field;
      iprot.ReadStructBegin();
      while (true)
      {
        field = iprot.ReadFieldBegin();
        if (field.Type == TType.Stop) { 
          break;
        }
        switch (field.ID)
        {
          case 1:
            if (field.Type == TType.I16) {
              ShardNumber = iprot.ReadI16();
              isset_ShardNumber = true;
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 2:
            if (field.Type == TType.List) {
              {
                Nodes = new List<string>();
                TList _list0 = iprot.ReadListBegin();
                for( int _i1 = 0; _i1 < _list0.Count; ++_i1)
                {
                  string _elem2 = null;
                  _elem2 = iprot.ReadString();
                  Nodes.Add(_elem2);
                }
                iprot.ReadListEnd();
              }
              isset_Nodes = true;
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          default: 
            TProtocolUtil.Skip(iprot, field.Type);
            break;
        }
        iprot.ReadFieldEnd();
      }
      iprot.ReadStructEnd();
      if (!isset_ShardNumber)
        throw new TProtocolException(TProtocolException.INVALID_DATA);
      if (!isset_Nodes)
        throw new TProtocolException(TProtocolException.INVALID_DATA);
    }

    public void Write(TProtocol oprot) {
      TStruct struc = new TStruct("ShardAllocationDetail");
      oprot.WriteStructBegin(struc);
      TField field = new TField();
      field.Name = "ShardNumber";
      field.Type = TType.I16;
      field.ID = 1;
      oprot.WriteFieldBegin(field);
      oprot.WriteI16(ShardNumber);
      oprot.WriteFieldEnd();
      field.Name = "Nodes";
      field.Type = TType.List;
      field.ID = 2;
      oprot.WriteFieldBegin(field);
      {
        oprot.WriteListBegin(new TList(TType.String, Nodes.Count));
        foreach (string _iter3 in Nodes)
        {
          oprot.WriteString(_iter3);
        }
        oprot.WriteListEnd();
      }
      oprot.WriteFieldEnd();
      oprot.WriteFieldStop();
      oprot.WriteStructEnd();
    }

    public override bool Equals(object that) {
      var other = that as ShardAllocationDetail;
      if (other == null) return false;
      if (ReferenceEquals(this, other)) return true;
      return System.Object.Equals(ShardNumber, other.ShardNumber)
        && TCollections.Equals(Nodes, other.Nodes);
    }

    public override int GetHashCode() {
      int hashcode = 0;
      unchecked {
        hashcode = (hashcode * 397) ^ ((ShardNumber.GetHashCode()));
        hashcode = (hashcode * 397) ^ ((TCollections.GetHashCode(Nodes)));
      }
      return hashcode;
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("ShardAllocationDetail(");
      sb.Append("ShardNumber: ");
      sb.Append(ShardNumber);
      sb.Append(",Nodes: ");
      sb.Append(Nodes);
      sb.Append(")");
      return sb.ToString();
    }

  }

}