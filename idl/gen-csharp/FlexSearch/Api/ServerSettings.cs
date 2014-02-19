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
using Thrift;
using Thrift.Collections;
using System.ServiceModel;
using System.Runtime.Serialization;
using Thrift.Protocol;
using Thrift.Transport;

namespace FlexSearch.Api
{

  #if !SILVERLIGHT
  [Serializable]
  #endif
  [DataContract(Namespace="")]
  public partial class ServerSettings : TBase
  {
    private int _HttpPort;
    private int _ThriftPort;
    private string _DataFolder;
    private string _PluginFolder;
    private string _ConfFolder;
    private string _NodeName;
    private NodeRole _NodeRole;

    [DataMember(Order = 1)]
    public int HttpPort
    {
      get
      {
        return _HttpPort;
      }
      set
      {
        __isset.HttpPort = true;
        this._HttpPort = value;
      }
    }

    [DataMember(Order = 2)]
    public int ThriftPort
    {
      get
      {
        return _ThriftPort;
      }
      set
      {
        __isset.ThriftPort = true;
        this._ThriftPort = value;
      }
    }

    [DataMember(Order = 3)]
    public string DataFolder
    {
      get
      {
        return _DataFolder;
      }
      set
      {
        __isset.DataFolder = true;
        this._DataFolder = value;
      }
    }

    [DataMember(Order = 4)]
    public string PluginFolder
    {
      get
      {
        return _PluginFolder;
      }
      set
      {
        __isset.PluginFolder = true;
        this._PluginFolder = value;
      }
    }

    [DataMember(Order = 5)]
    public string ConfFolder
    {
      get
      {
        return _ConfFolder;
      }
      set
      {
        __isset.ConfFolder = true;
        this._ConfFolder = value;
      }
    }

    [DataMember(Order = 6)]
    public string NodeName
    {
      get
      {
        return _NodeName;
      }
      set
      {
        __isset.NodeName = true;
        this._NodeName = value;
      }
    }

    /// <summary>
    /// 
    /// <seealso cref="NodeRole"/>
    /// </summary>
    [DataMember(Order = 7)]
    public NodeRole NodeRole
    {
      get
      {
        return _NodeRole;
      }
      set
      {
        __isset.NodeRole = true;
        this._NodeRole = value;
      }
    }


    public Isset __isset;
    #if !SILVERLIGHT
    [Serializable]
    #endif
    [DataContract]
    public struct Isset {
      public bool HttpPort;
      public bool ThriftPort;
      public bool DataFolder;
      public bool PluginFolder;
      public bool ConfFolder;
      public bool NodeName;
      public bool NodeRole;
    }

    public ServerSettings() {
      this._HttpPort = 9800;
      this.__isset.HttpPort = true;
      this._ThriftPort = 9900;
      this.__isset.ThriftPort = true;
      this._DataFolder = "./data";
      this.__isset.DataFolder = true;
      this._PluginFolder = "./plugins";
      this.__isset.PluginFolder = true;
      this._ConfFolder = "./conf";
      this.__isset.ConfFolder = true;
      this._NodeName = "FlexNode";
      this.__isset.NodeName = true;
      this._NodeRole = NodeRole.Master;
      this.__isset.NodeRole = true;
    }

    public void Read (TProtocol iprot)
    {
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
            if (field.Type == TType.I32) {
              HttpPort = iprot.ReadI32();
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 2:
            if (field.Type == TType.I32) {
              ThriftPort = iprot.ReadI32();
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 3:
            if (field.Type == TType.String) {
              DataFolder = iprot.ReadString();
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 4:
            if (field.Type == TType.String) {
              PluginFolder = iprot.ReadString();
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 5:
            if (field.Type == TType.String) {
              ConfFolder = iprot.ReadString();
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 6:
            if (field.Type == TType.String) {
              NodeName = iprot.ReadString();
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 7:
            if (field.Type == TType.I32) {
              NodeRole = (NodeRole)iprot.ReadI32();
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
    }

    public void Write(TProtocol oprot) {
      TStruct struc = new TStruct("ServerSettings");
      oprot.WriteStructBegin(struc);
      TField field = new TField();
      if (__isset.HttpPort) {
        field.Name = "HttpPort";
        field.Type = TType.I32;
        field.ID = 1;
        oprot.WriteFieldBegin(field);
        oprot.WriteI32(HttpPort);
        oprot.WriteFieldEnd();
      }
      if (__isset.ThriftPort) {
        field.Name = "ThriftPort";
        field.Type = TType.I32;
        field.ID = 2;
        oprot.WriteFieldBegin(field);
        oprot.WriteI32(ThriftPort);
        oprot.WriteFieldEnd();
      }
      if (DataFolder != null && __isset.DataFolder) {
        field.Name = "DataFolder";
        field.Type = TType.String;
        field.ID = 3;
        oprot.WriteFieldBegin(field);
        oprot.WriteString(DataFolder);
        oprot.WriteFieldEnd();
      }
      if (PluginFolder != null && __isset.PluginFolder) {
        field.Name = "PluginFolder";
        field.Type = TType.String;
        field.ID = 4;
        oprot.WriteFieldBegin(field);
        oprot.WriteString(PluginFolder);
        oprot.WriteFieldEnd();
      }
      if (ConfFolder != null && __isset.ConfFolder) {
        field.Name = "ConfFolder";
        field.Type = TType.String;
        field.ID = 5;
        oprot.WriteFieldBegin(field);
        oprot.WriteString(ConfFolder);
        oprot.WriteFieldEnd();
      }
      if (NodeName != null && __isset.NodeName) {
        field.Name = "NodeName";
        field.Type = TType.String;
        field.ID = 6;
        oprot.WriteFieldBegin(field);
        oprot.WriteString(NodeName);
        oprot.WriteFieldEnd();
      }
      if (__isset.NodeRole) {
        field.Name = "NodeRole";
        field.Type = TType.I32;
        field.ID = 7;
        oprot.WriteFieldBegin(field);
        oprot.WriteI32((int)NodeRole);
        oprot.WriteFieldEnd();
      }
      oprot.WriteFieldStop();
      oprot.WriteStructEnd();
    }

    public override bool Equals(object that) {
      var other = that as ServerSettings;
      if (other == null) return false;
      if (ReferenceEquals(this, other)) return true;
      return ((__isset.HttpPort == other.__isset.HttpPort) && ((!__isset.HttpPort) || (System.Object.Equals(HttpPort, other.HttpPort))))
        && ((__isset.ThriftPort == other.__isset.ThriftPort) && ((!__isset.ThriftPort) || (System.Object.Equals(ThriftPort, other.ThriftPort))))
        && ((__isset.DataFolder == other.__isset.DataFolder) && ((!__isset.DataFolder) || (System.Object.Equals(DataFolder, other.DataFolder))))
        && ((__isset.PluginFolder == other.__isset.PluginFolder) && ((!__isset.PluginFolder) || (System.Object.Equals(PluginFolder, other.PluginFolder))))
        && ((__isset.ConfFolder == other.__isset.ConfFolder) && ((!__isset.ConfFolder) || (System.Object.Equals(ConfFolder, other.ConfFolder))))
        && ((__isset.NodeName == other.__isset.NodeName) && ((!__isset.NodeName) || (System.Object.Equals(NodeName, other.NodeName))))
        && ((__isset.NodeRole == other.__isset.NodeRole) && ((!__isset.NodeRole) || (System.Object.Equals(NodeRole, other.NodeRole))));
    }

    public override int GetHashCode() {
      int hashcode = 0;
      unchecked {
        hashcode = (hashcode * 397) ^ (!__isset.HttpPort ? 0 : (HttpPort.GetHashCode()));
        hashcode = (hashcode * 397) ^ (!__isset.ThriftPort ? 0 : (ThriftPort.GetHashCode()));
        hashcode = (hashcode * 397) ^ (!__isset.DataFolder ? 0 : (DataFolder.GetHashCode()));
        hashcode = (hashcode * 397) ^ (!__isset.PluginFolder ? 0 : (PluginFolder.GetHashCode()));
        hashcode = (hashcode * 397) ^ (!__isset.ConfFolder ? 0 : (ConfFolder.GetHashCode()));
        hashcode = (hashcode * 397) ^ (!__isset.NodeName ? 0 : (NodeName.GetHashCode()));
        hashcode = (hashcode * 397) ^ (!__isset.NodeRole ? 0 : (NodeRole.GetHashCode()));
      }
      return hashcode;
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("ServerSettings(");
      sb.Append("HttpPort: ");
      sb.Append(HttpPort);
      sb.Append(",ThriftPort: ");
      sb.Append(ThriftPort);
      sb.Append(",DataFolder: ");
      sb.Append(DataFolder);
      sb.Append(",PluginFolder: ");
      sb.Append(PluginFolder);
      sb.Append(",ConfFolder: ");
      sb.Append(ConfFolder);
      sb.Append(",NodeName: ");
      sb.Append(NodeName);
      sb.Append(",NodeRole: ");
      sb.Append(NodeRole);
      sb.Append(")");
      return sb.ToString();
    }

  }

}
