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
package FlexSearch.Java;

import java.util.HashMap;
import org.apache.lucene.codecs.DocValuesFormat;
import org.apache.lucene.codecs.lucene49.Lucene49DocValuesFormat;
import org.apache.lucene.codecs.lucene410.Lucene410DocValuesFormat;
import org.apache.lucene.codecs.memory.DirectDocValuesFormat;
import org.apache.lucene.codecs.memory.MemoryDocValuesFormat;
import org.apache.lucene.codecs.perfield.PerFieldDocValuesFormat;

public class FlexPerFieldDocValuesFormat extends PerFieldDocValuesFormat {

    private final HashMap<String, DocValuesFormat> map = new HashMap<String, DocValuesFormat>();

    public FlexPerFieldDocValuesFormat() {
        map.put("lucene_4_10", new Lucene410DocValuesFormat());
        // Lucene49DocValuesFormat
        map.put("lucene_4_9", new Lucene49DocValuesFormat());
        // DirectDocValuesFormat
        map.put("direct", new DirectDocValuesFormat());
        // MemoryDocValuesFormat
        map.put("memory", new MemoryDocValuesFormat());
    }

    @Override
    public DocValuesFormat getDocValuesFormatForField(String fieldName) {
        return map.get(fieldName.substring(fieldName.indexOf("[") + 1, fieldName.indexOf("]")));
    }

}
