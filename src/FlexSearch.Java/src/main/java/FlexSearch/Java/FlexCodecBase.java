// ----------------------------------------------------------------------------
// (c) Seemant Rajvanshi, 2014
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
import org.apache.lucene.codecs.lucene410.Lucene410Codec;

public abstract class FlexCodecBase extends FilterCodec {

    private static final PostingsFormat postingsFormat = new FlexPerFieldPostingsFormat();

    public FlexCodecBase(String codecName, Codec wrappingCodec) {
        super(codecName, wrappingCodec);
    }

    @Override
    public final PostingsFormat postingsFormat() {
        return postingsFormat;
    }
}
