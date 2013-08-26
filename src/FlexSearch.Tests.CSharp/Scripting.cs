using CSScriptLibrary;
using System;

//Read in more details about all aspects of CS-Script hosting in applications here: http://csscript.net/help/evaluator.html
//You can also find the example of all possible hosting scenarios (including debugging) in the <cs-script>\Samples\Hosting\CompilerAsService\HostingScenarios.cs sample 
//from the installed files. Alternatively the file can be accessed from here https://dl.dropbox.com/u/2192462/CS-S/HomePageReferences/HostingScenarios.cs

public interface ICalc
{
    HostApp Host { get; set; }
    int Sum(int a, int b); 
}

public class HostApp
{
    static void Test()
    {
        var host = new HostApp();
        
        host.CalcTest_InterfaceAlignment();
        host.CalcTest_InterfaceInheritance();
        host.HelloTest();
    }

    void HelloTest()
    {
        dynamic script = CSScript.Evaluator
                                 .LoadMethod(@"void SayHello(string greeting)
                                               {
                                                   Console.WriteLine(greeting);
                                               }");

        script.SayHello("Hello World!");
     }

    void CalcTest_InterfaceInheritance()
    {
        ICalc calc = (ICalc)CSScript.Evaluator
                                    .LoadCode(@"public class Script : ICalc
                                                { 
                                                    public int Sum(int a, int b)
                                                    {
                                                        if(Host != null) 
                                                            Host.Log(""Sum is invoked"");
                                                        return a + b;
                                                    }
                             
                                                    public HostApp Host { get; set; }
                                                }");
        calc.Host = this;                             
        int result = calc.Sum(1, 2);
    }
    
    void CalcTest_InterfaceAlignment()
    {
        ICalc calc = CSScript.Evaluator
                             .LoadMethod<ICalc>(@"public int Sum(int a, int b)
                                                  {
                                                      if(Host != null) 
                                                          Host.Log(""Sum is invoked"");
                                                      return a + b;
                                                  }

                                                  public HostApp Host { get; set; }");
        calc.Host = this;
        int result = calc.Sum(1, 2);
    }
    
    public void Log(string message)
    {
        Console.WriteLine(message);
    }
}