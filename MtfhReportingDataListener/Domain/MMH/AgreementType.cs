// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by avrogen, version 1.7.7.5
//    Changes to this file may cause incorrect behavior and will be lost if code
//    is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------
namespace MMH
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using global::Avro;
	using global::Avro.Specific;
	
	public partial class AgreementType : ISpecificRecord
	{
		public static Schema _SCHEMA = Schema.Parse("{\"type\":\"record\",\"name\":\"AgreementType\",\"namespace\":\"MMH\",\"fields\":[{\"name\":\"Code" +
				"\",\"type\":\"string\"},{\"name\":\"Description\",\"type\":\"string\"}]}");
		private string _Code;
		private string _Description;
		public virtual Schema Schema
		{
			get
			{
				return AgreementType._SCHEMA;
			}
		}
		public string Code
		{
			get
			{
				return this._Code;
			}
			set
			{
				this._Code = value;
			}
		}
		public string Description
		{
			get
			{
				return this._Description;
			}
			set
			{
				this._Description = value;
			}
		}
		public virtual object Get(int fieldPos)
		{
			switch (fieldPos)
			{
			case 0: return this.Code;
			case 1: return this.Description;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
			};
		}
		public virtual void Put(int fieldPos, object fieldValue)
		{
			switch (fieldPos)
			{
			case 0: this.Code = (System.String)fieldValue; break;
			case 1: this.Description = (System.String)fieldValue; break;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
			};
		}
	}
}