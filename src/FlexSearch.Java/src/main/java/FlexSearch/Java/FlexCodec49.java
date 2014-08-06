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

import org.apache.lucene.codecs.*;
import org.apache.lucene.codecs.lucene49.Lucene49Codec;

public final class FlexCodec49 extends FilterCodec {

    private final PostingsFormat postingsFormat = new FlexPerFieldPostingsFormat();
    private final DocValuesFormat docValuesFormat = new FlexPerFieldDocValuesFormat();

    public FlexCodec49() {
        super("FlexCodec49", new Lucene49Codec());
    }

    @Override
    public final PostingsFormat postingsFormat() {
        return postingsFormat;
    }

    @Override
    public final DocValuesFormat docValuesFormat() {
        return docValuesFormat;
    }
}
