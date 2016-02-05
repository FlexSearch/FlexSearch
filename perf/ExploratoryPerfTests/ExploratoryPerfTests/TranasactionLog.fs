module TranasactionLog

open PerfUtil
open System.Threading
open System.Threading.Tasks
open System
open System
open System.IO
open System.Security.AccessControl
open MsgPack.Serialization
open System.Text

type TestClass() = 
    member val TxId : int64 = 0L with get, set
    member val TestString : string = """test
    on
    multiple
    lines
    """ with get, set
    member val TestInt : int = 100000000 with get, set

[<LiteralAttribute>]
let SOH = 1uy

[<LiteralAttribute>]
let STX = 2uy

[<LiteralAttribute>]
let ETX = 3uy

[<LiteralAttribute>]
let EOT = 4uy

let getEncoding (num : int64) (writer : BinaryWriter) = 
    let a = Encoding.UTF8.GetBytes(num.ToString())
    writer.Write(SOH)
    writer.Write(STX)
    writer.Write(STX)
    writer.Write(STX)
    writer.Write(STX)
    writer.Write(num)
    writer.Write(ETX)
    writer.Write(ETX)
    writer.Write(ETX)
    writer.Write(ETX)
    writer.Write(EOT)

let (|Array|_|) pattern toMatch = 
    let patternLength = Array.length pattern
    let toMatchLength = Array.length toMatch
    let tail = Array.sub toMatch patternLength (toMatchLength - patternLength)
    let completePattern = Array.concat [ pattern; tail ]
    if toMatch = completePattern then Some(tail)
    else None

let BinaryWriterTest() = 
    let tempPath = Path.GetTempFileName()
    printfn "File location: %s" tempPath
    use fs = 
        new FileStream(@"C:\temp\test1.txt", FileMode.OpenOrCreate, FileSystemRights.AppendData, FileShare.Write, 1024, 
                       FileOptions.Asynchronous)
    use writer = new BinaryWriter(fs, UTF8Encoding.Default)
    let serializer = SerializationContext.Default.GetSerializer<TestClass>()
    let stream = new MemoryStream()
    let value = serializer.Pack(stream, new TestClass())
    let result = stream.ToArray()
    writer |> getEncoding 1L
    writer.Write(result, 0, result.Length)
    writer |> getEncoding 2L
    writer.Write(result, 0, result.Length)
    writer |> getEncoding 3L
    writer.Write(result, 0, result.Length)
    writer |> getEncoding 4L
    writer.Close()
    fs.Close()
    // Let's open it and read the stuff
    let bytes = File.ReadAllBytes(@"C:\temp\test1.txt")
    match bytes with
    | Array [| SOH; STX; STX; STX; STX |] tail -> 
        printfn "Found Start of a record"
        printfn "Tail %A" tail
    | _ -> ()
