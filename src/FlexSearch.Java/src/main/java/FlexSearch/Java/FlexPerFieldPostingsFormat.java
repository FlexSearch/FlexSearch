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
import org.apache.lucene.codecs.PostingsFormat;
import org.apache.lucene.codecs.bloom.BloomFilteringPostingsFormat;
import org.apache.lucene.codecs.lucene41.Lucene41PostingsFormat;
import org.apache.lucene.codecs.memory.DirectPostingsFormat;
import org.apache.lucene.codecs.memory.MemoryPostingsFormat;
import org.apache.lucene.codecs.perfield.PerFieldPostingsFormat;
import org.apache.lucene.codecs.pulsing.Pulsing41PostingsFormat;

class FlexPerFieldPostingsFormat extends PerFieldPostingsFormat {

    private final static HashMap<String, PostingsFormat> map = new HashMap<String, PostingsFormat>();

    static {
        // Memory postings
        map.put("memory", new MemoryPostingsFormat());
        // Direct postings
        map.put("direct", new DirectPostingsFormat());
        // Bloom_4_1 postings
        map.put("bloom_4_1", new BloomFilteringPostingsFormat(new Lucene41PostingsFormat()));
        // Lucene_4_1 postings
        map.put("lucene_4_1", new Lucene41PostingsFormat());
        // Pulsing_4_1 postings
        map.put("pulsing_4_1", new Pulsing41PostingsFormat());
    }

    public FlexPerFieldPostingsFormat() {
        assert (map.size() != 0);
    }

    @Override
    public PostingsFormat getPostingsFormatForField(String fieldName) {
        return map.get(fieldName.substring(fieldName.indexOf("<") + 1, fieldName.indexOf(">")));
    }
}
