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

// ----------------------------------------------------------------------------
namespace FlexSearch.Core
// ----------------------------------------------------------------------------

[<AutoOpen>]
module Interop =
    open System
    open System.Linq
    open System.Runtime.InteropServices
    open System.Reflection
    open System.Threading
    

    type HTTP_SERVER_PROPERTY =
        | HttpServerAuthenticationProperty = 0
        | HttpServerLoggingProperty = 1
        | HttpServerQosProperty = 2
        | HttpServerTimeoutsProperty = 3
        | HttpServerQueueLengthProperty = 4
        | HttpServerStateProperty = 5
        | HttpServer503VerbosityProperty = 6
        | HttpServerBindingProperty = 7
        | HttpServerExtendedAuthenticationProperty = 8
        | HttpServerListenEndpointProperty = 9
        | HttpServerChannelBindProperty = 10
        | HttpServerProtectionLevelProperty = 11


    [<DllImport("httpapi.dll", CallingConvention = CallingConvention.StdCall)>]
    extern uint32 HttpSetRequestQueueProperty(
            CriticalHandle requestQueueHandle,
            HTTP_SERVER_PROPERTY serverProperty,
            uint32& pPropertyInfo,
            uint32 propertyInfoLength,
            uint32 reserved,
            IntPtr pReserved)


    // Adapted from:
    // http://stackoverflow.com/questions/15417062/changing-http-sys-kernel-queue-limit-when-using-net-httplistener
    /// Sets the request queue length of a http listener
    let SetRequestQueueLength(listener: System.Net.HttpListener, len: uint32) =
        let listenerType = typeof<System.Net.HttpListener>
        let mutable length = len
        let requestQueueHandleProperty = 
            listenerType.GetProperties(BindingFlags.NonPublic ||| BindingFlags.Instance).First(fun p -> p.Name = "RequestQueueHandle")

        let requestQueueHandle = requestQueueHandleProperty.GetValue(listener) :?> CriticalHandle
        let result = HttpSetRequestQueueProperty(requestQueueHandle, HTTP_SERVER_PROPERTY.HttpServerQueueLengthProperty, &length, Marshal.SizeOf(len) |> uint32, 0u, IntPtr.Zero);

        if result <> 0u then
            failwithf ""
    

    let MaximizeThreads() =
        // To improve CPU utilization, increase the number of threads that the .NET thread pool expands by when
        // a burst of requests come in. We could do this by editing machine.config/system.web/processModel/minWorkerThreads,
        // but that seems too global a change, so we do it in code for just our AppPool. More info:
        
        // http://support.microsoft.com/kb/821268
        // http://blogs.msdn.com/b/tmarq/archive/2007/07/21/asp-net-thread-usage-on-iis-7-0-and-6-0.aspx
        // http://blogs.msdn.com/b/perfworld/archive/2010/01/13/how-can-i-improve-the-performance-of-asp-net-by-adjusting-the-clr-thread-throttling-properties.aspx

        let newMinWorkerThreads = 10;
        let (minWorkerThreads, minCompletionPortThreads) = ThreadPool.GetMinThreads()
        ThreadPool.SetMinThreads(Environment.ProcessorCount * newMinWorkerThreads, minCompletionPortThreads)
       