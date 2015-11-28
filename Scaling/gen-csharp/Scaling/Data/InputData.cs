/**
 * Autogenerated by Thrift Compiler (1.0.0-dev)
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
using System.Runtime.Serialization;
using Thrift.Protocol;
using Thrift.Transport;

namespace Scaling.Data
{

  #if !SILVERLIGHT
  [Serializable]
  #endif
  public partial class InputData : TBase
  {
    private double _SecondOperand;

    public double SecondOperand
    {
      get
      {
        return _SecondOperand;
      }
      set
      {
        __isset.SecondOperand = true;
        this._SecondOperand = value;
      }
    }


    public Isset __isset;
    #if !SILVERLIGHT
    [Serializable]
    #endif
    public struct Isset {
      public bool SecondOperand;
    }

    public InputData() {
    }

    public void Read (TProtocol iprot)
    {
      iprot.IncrementRecursionDepth();
      try
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
              if (field.Type == TType.Double) {
                SecondOperand = iprot.ReadDouble();
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
      finally
      {
        iprot.DecrementRecursionDepth();
      }
    }

    public void Write(TProtocol oprot) {
      oprot.IncrementRecursionDepth();
      try
      {
        TStruct struc = new TStruct("InputData");
        oprot.WriteStructBegin(struc);
        TField field = new TField();
        if (__isset.SecondOperand) {
          field.Name = "SecondOperand";
          field.Type = TType.Double;
          field.ID = 1;
          oprot.WriteFieldBegin(field);
          oprot.WriteDouble(SecondOperand);
          oprot.WriteFieldEnd();
        }
        oprot.WriteFieldStop();
        oprot.WriteStructEnd();
      }
      finally
      {
        oprot.DecrementRecursionDepth();
      }
    }

    public override string ToString() {
      StringBuilder __sb = new StringBuilder("InputData(");
      bool __first = true;
      if (__isset.SecondOperand) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("SecondOperand: ");
        __sb.Append(SecondOperand);
      }
      __sb.Append(")");
      return __sb.ToString();
    }

  }

}
