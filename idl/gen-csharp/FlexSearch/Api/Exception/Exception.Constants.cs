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

namespace FlexSearch.Api.Exception
{
  public static class ExceptionConstants
  {
    public static InvalidOperation INDEX_NOT_FOUND = new InvalidOperation();
    public static InvalidOperation INDEX_ALREADY_EXISTS = new InvalidOperation();
    public static InvalidOperation INDEX_SHOULD_BE_OFFLINE = new InvalidOperation();
    static ExceptionConstants()
    {
      INDEX_NOT_FOUND.DeveloperMessage = "The requested index does not exist.";
      INDEX_NOT_FOUND.UserMessage = "The requested index does not exist.";
      INDEX_NOT_FOUND.ErrorCode = 1000;
      INDEX_ALREADY_EXISTS.DeveloperMessage = "The requested index already exist.";
      INDEX_ALREADY_EXISTS.UserMessage = "The requested index already exist.";
      INDEX_ALREADY_EXISTS.ErrorCode = 1002;
      INDEX_SHOULD_BE_OFFLINE.DeveloperMessage = "Index should be made offline before attempting to update index settings.";
      INDEX_SHOULD_BE_OFFLINE.UserMessage = "Index should be made offline before attempting the operation.";
      INDEX_SHOULD_BE_OFFLINE.ErrorCode = 1003;
    }
  }
}
