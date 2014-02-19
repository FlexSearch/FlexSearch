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

namespace FlexSearch.Api.Exception
{

  #if !SILVERLIGHT
  [Serializable]
  #endif
  [DataContract(Namespace="")]
  public partial class InvalidOperation : TBase
  {
    private string _DeveloperMessage;
    private string _UserMessage;
    private int _ErrorCode;

    [DataMember(Order = 1)]
    public string DeveloperMessage
    {
      get
      {
        return _DeveloperMessage;
      }
      set
      {
        __isset.DeveloperMessage = true;
        this._DeveloperMessage = value;
      }
    }

    [DataMember(Order = 2)]
    public string UserMessage
    {
      get
      {
        return _UserMessage;
      }
      set
      {
        __isset.UserMessage = true;
        this._UserMessage = value;
      }
    }

    [DataMember(Order = 3)]
    public int ErrorCode
    {
      get
      {
        return _ErrorCode;
      }
      set
      {
        __isset.ErrorCode = true;
        this._ErrorCode = value;
      }
    }


    public Isset __isset;
    #if !SILVERLIGHT
    [Serializable]
    #endif
    [DataContract]
    public struct Isset {
      public bool DeveloperMessage;
      public bool UserMessage;
      public bool ErrorCode;
    }

    public InvalidOperation() {
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
            if (field.Type == TType.String) {
              DeveloperMessage = iprot.ReadString();
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 2:
            if (field.Type == TType.String) {
              UserMessage = iprot.ReadString();
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 3:
            if (field.Type == TType.I32) {
              ErrorCode = iprot.ReadI32();
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
      TStruct struc = new TStruct("InvalidOperation");
      oprot.WriteStructBegin(struc);
      TField field = new TField();
      if (DeveloperMessage != null && __isset.DeveloperMessage) {
        field.Name = "DeveloperMessage";
        field.Type = TType.String;
        field.ID = 1;
        oprot.WriteFieldBegin(field);
        oprot.WriteString(DeveloperMessage);
        oprot.WriteFieldEnd();
      }
      if (UserMessage != null && __isset.UserMessage) {
        field.Name = "UserMessage";
        field.Type = TType.String;
        field.ID = 2;
        oprot.WriteFieldBegin(field);
        oprot.WriteString(UserMessage);
        oprot.WriteFieldEnd();
      }
      if (__isset.ErrorCode) {
        field.Name = "ErrorCode";
        field.Type = TType.I32;
        field.ID = 3;
        oprot.WriteFieldBegin(field);
        oprot.WriteI32(ErrorCode);
        oprot.WriteFieldEnd();
      }
      oprot.WriteFieldStop();
      oprot.WriteStructEnd();
    }

    public override bool Equals(object that) {
      var other = that as InvalidOperation;
      if (other == null) return false;
      if (ReferenceEquals(this, other)) return true;
      return ((__isset.DeveloperMessage == other.__isset.DeveloperMessage) && ((!__isset.DeveloperMessage) || (System.Object.Equals(DeveloperMessage, other.DeveloperMessage))))
        && ((__isset.UserMessage == other.__isset.UserMessage) && ((!__isset.UserMessage) || (System.Object.Equals(UserMessage, other.UserMessage))))
        && ((__isset.ErrorCode == other.__isset.ErrorCode) && ((!__isset.ErrorCode) || (System.Object.Equals(ErrorCode, other.ErrorCode))));
    }

    public override int GetHashCode() {
      int hashcode = 0;
      unchecked {
        hashcode = (hashcode * 397) ^ (!__isset.DeveloperMessage ? 0 : (DeveloperMessage.GetHashCode()));
        hashcode = (hashcode * 397) ^ (!__isset.UserMessage ? 0 : (UserMessage.GetHashCode()));
        hashcode = (hashcode * 397) ^ (!__isset.ErrorCode ? 0 : (ErrorCode.GetHashCode()));
      }
      return hashcode;
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("InvalidOperation(");
      sb.Append("DeveloperMessage: ");
      sb.Append(DeveloperMessage);
      sb.Append(",UserMessage: ");
      sb.Append(UserMessage);
      sb.Append(",ErrorCode: ");
      sb.Append(ErrorCode);
      sb.Append(")");
      return sb.ToString();
    }

  }

}
