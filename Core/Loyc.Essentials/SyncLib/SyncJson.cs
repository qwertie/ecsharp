using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

#nullable enable

namespace Loyc.SyncLib
{

    // TODO: review Json.NET (NewtonSoft.Json?) which "does have support for referential types containing cyclic relationships"
    // https://trycatch.me/xml-json-serialization-of-object-graphs-with-cyclic-references-in-net/
    //public static partial class SyncJson
    //{
    //    static readonly Symbol _typeMarker = (Symbol)"\t";
    //    static readonly Symbol _objectRef = (Symbol)"\r";
    //    //public SyncTypeRegistry? TypeRegistry { get; set; }
	//
    //    public SyncJson() { }
	//
    //    public void Save<T>(Stream stream, T rootObject)
    //    {
    //        // Goal:
    //        //{
    //        //    "\r": 1, // reference
    //        //    "\t": "Person", // type
    //        //    "Name": "Jack",
    //        //    "Age": 11,
    //        //    "Siblings": [
    //        //        { "\r": 2,
    //        //          "Name": "Jill",
    //        //          "Age": 9,
    //        //          "Siblings": [{ "\r": 1 }]
    //        //        }
    //        //    ]
    //        //}
	//
    //        var sync = new Synchronizer(stream, TypeRegistry ?? SyncTypeRegistry.Default);
    //        sync.Sync(null, rootObject);
    //    }
	//
    //    private SyncObjectFunc<JsonWriter, T> GetSyncObjectFunc<T>()
    //    {
    //    }
    //}
}
