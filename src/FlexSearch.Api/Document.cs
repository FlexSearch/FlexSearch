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
  public partial class Document : TBase
  {
    private Dictionary<string, string> _Fields;
    private List<string> _Highlights;
    private long _LastModified;
    private double _Score;

    [DataMember]
    public Dictionary<string, string> Fields
    {
      get
      {
        return _Fields;
      }
      set
      {
        __isset.Fields = true;
        this._Fields = value;
      }
    }

    [DataMember]
    public List<string> Highlights
    {
      get
      {
        return _Highlights;
      }
      set
      {
        __isset.Highlights = true;
        this._Highlights = value;
      }
    }

    [DataMember]
    public string Id { get; set; }

    [DataMember]
    public long LastModified
    {
      get
      {
        return _LastModified;
      }
      set
      {
        __isset.LastModified = true;
        this._LastModified = value;
      }
    }

    [DataMember]
    public int Version { get; set; }

    [DataMember]
    public string Index { get; set; }

    [DataMember]
    public double Score
    {
      get
      {
        return _Score;
      }
      set
      {
        __isset.Score = true;
        this._Score = value;
      }
    }


    public Isset __isset;
    #if !SILVERLIGHT
    [Serializable]
    #endif
    [DataContract]
    public struct Isset {
      public bool Fields;
      public bool Highlights;
      public bool LastModified;
      public bool Score;
    }

    public Document() {
      this._Fields = new Dictionary<string, string>();
      this.__isset.Fields = true;
      this._Highlights = new List<string>();
      this.__isset.Highlights = true;
      this.Version = 1;
      this._Score = 0;
      this.__isset.Score = true;
    }

    public Document(string Id, int Version, string Index) : this() {
      this.Id = Id;
      this.Version = Version;
      this.Index = Index;
    }

    public void Read (TProtocol iprot)
    {
      bool isset_Id = false;
      bool isset_Version = false;
      bool isset_Index = false;
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
            if (field.Type == TType.Map) {
              {
                Fields = new Dictionary<string, string>();
                TMap _map34 = iprot.ReadMapBegin();
                for( int _i35 = 0; _i35 < _map34.Count; ++_i35)
                {
                  string _key36;
                  string _val37;
                  _key36 = iprot.ReadString();
                  _val37 = iprot.ReadString();
                  Fields[_key36] = _val37;
                }
                iprot.ReadMapEnd();
              }
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 2:
            if (field.Type == TType.List) {
              {
                Highlights = new List<string>();
                TList _list38 = iprot.ReadListBegin();
                for( int _i39 = 0; _i39 < _list38.Count; ++_i39)
                {
                  string _elem40 = null;
                  _elem40 = iprot.ReadString();
                  Highlights.Add(_elem40);
                }
                iprot.ReadListEnd();
              }
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 3:
            if (field.Type == TType.String) {
              Id = iprot.ReadString();
              isset_Id = true;
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 4:
            if (field.Type == TType.I64) {
              LastModified = iprot.ReadI64();
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 5:
            if (field.Type == TType.I32) {
              Version = iprot.ReadI32();
              isset_Version = true;
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 7:
            if (field.Type == TType.String) {
              Index = iprot.ReadString();
              isset_Index = true;
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 8:
            if (field.Type == TType.Double) {
              Score = iprot.ReadDouble();
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
      if (!isset_Id)
        throw new TProtocolException(TProtocolException.INVALID_DATA);
      if (!isset_Version)
        throw new TProtocolException(TProtocolException.INVALID_DATA);
      if (!isset_Index)
        throw new TProtocolException(TProtocolException.INVALID_DATA);
    }

    public void Write(TProtocol oprot) {
      TStruct struc = new TStruct("Document");
      oprot.WriteStructBegin(struc);
      TField field = new TField();
      if (Fields != null && __isset.Fields) {
        field.Name = "Fields";
        field.Type = TType.Map;
        field.ID = 1;
        oprot.WriteFieldBegin(field);
        {
          oprot.WriteMapBegin(new TMap(TType.String, TType.String, Fields.Count));
          foreach (string _iter41 in Fields.Keys)
          {
            oprot.WriteString(_iter41);
            oprot.WriteString(Fields[_iter41]);
          }
          oprot.WriteMapEnd();
        }
        oprot.WriteFieldEnd();
      }
      if (Highlights != null && __isset.Highlights) {
        field.Name = "Highlights";
        field.Type = TType.List;
        field.ID = 2;
        oprot.WriteFieldBegin(field);
        {
          oprot.WriteListBegin(new TList(TType.String, Highlights.Count));
          foreach (string _iter42 in Highlights)
          {
            oprot.WriteString(_iter42);
          }
          oprot.WriteListEnd();
        }
        oprot.WriteFieldEnd();
      }
      field.Name = "Id";
      field.Type = TType.String;
      field.ID = 3;
      oprot.WriteFieldBegin(field);
      oprot.WriteString(Id);
      oprot.WriteFieldEnd();
      if (__isset.LastModified) {
        field.Name = "LastModified";
        field.Type = TType.I64;
        field.ID = 4;
        oprot.WriteFieldBegin(field);
        oprot.WriteI64(LastModified);
        oprot.WriteFieldEnd();
      }
      field.Name = "Version";
      field.Type = TType.I32;
      field.ID = 5;
      oprot.WriteFieldBegin(field);
      oprot.WriteI32(Version);
      oprot.WriteFieldEnd();
      field.Name = "Index";
      field.Type = TType.String;
      field.ID = 7;
      oprot.WriteFieldBegin(field);
      oprot.WriteString(Index);
      oprot.WriteFieldEnd();
      if (__isset.Score) {
        field.Name = "Score";
        field.Type = TType.Double;
        field.ID = 8;
        oprot.WriteFieldBegin(field);
        oprot.WriteDouble(Score);
        oprot.WriteFieldEnd();
      }
      oprot.WriteFieldStop();
      oprot.WriteStructEnd();
    }

    public override bool Equals(object that) {
      var other = that as Document;
      if (other == null) return false;
      if (ReferenceEquals(this, other)) return true;
      return ((__isset.Fields == other.__isset.Fields) && ((!__isset.Fields) || (TCollections.Equals(Fields, other.Fields))))
        && ((__isset.Highlights == other.__isset.Highlights) && ((!__isset.Highlights) || (TCollections.Equals(Highlights, other.Highlights))))
        && System.Object.Equals(Id, other.Id)
        && ((__isset.LastModified == other.__isset.LastModified) && ((!__isset.LastModified) || (System.Object.Equals(LastModified, other.LastModified))))
        && System.Object.Equals(Version, other.Version)
        && System.Object.Equals(Index, other.Index)
        && ((__isset.Score == other.__isset.Score) && ((!__isset.Score) || (System.Object.Equals(Score, other.Score))));
    }

    public override int GetHashCode() {
      int hashcode = 0;
      unchecked {
        hashcode = (hashcode * 397) ^ (!__isset.Fields ? 0 : (TCollections.GetHashCode(Fields)));
        hashcode = (hashcode * 397) ^ (!__isset.Highlights ? 0 : (TCollections.GetHashCode(Highlights)));
        hashcode = (hashcode * 397) ^ ((Id.GetHashCode()));
        hashcode = (hashcode * 397) ^ (!__isset.LastModified ? 0 : (LastModified.GetHashCode()));
        hashcode = (hashcode * 397) ^ ((Version.GetHashCode()));
        hashcode = (hashcode * 397) ^ ((Index.GetHashCode()));
        hashcode = (hashcode * 397) ^ (!__isset.Score ? 0 : (Score.GetHashCode()));
      }
      return hashcode;
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("Document(");
      sb.Append("Fields: ");
      sb.Append(Fields);
      sb.Append(",Highlights: ");
      sb.Append(Highlights);
      sb.Append(",Id: ");
      sb.Append(Id);
      sb.Append(",LastModified: ");
      sb.Append(LastModified);
      sb.Append(",Version: ");
      sb.Append(Version);
      sb.Append(",Index: ");
      sb.Append(Index);
      sb.Append(",Score: ");
      sb.Append(Score);
      sb.Append(")");
      return sb.ToString();
    }

  }

}