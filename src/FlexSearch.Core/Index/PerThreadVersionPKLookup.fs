// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2013
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core
open org.apache.lucene.index
open org.apache.lucene.sandbox
open org.apache.lucene.codecs.idversion
open org.apache.lucene.document
open org.apache.lucene.analysis
open org.apache.lucene.document

[<AbstractClass; Sealed>] 
type IdFieldHelpers() =
    static let idField = 
        let fieldType = new FieldType()
        fieldType.setIndexed(true)
        fieldType.setOmitNorms(true)
        fieldType.setIndexOptions(FieldInfo.IndexOptions.DOCS_ONLY)
        fieldType.setTokenized(false)
        fieldType.freeze()
        fieldType

    static member IdFieldType = idField
      

