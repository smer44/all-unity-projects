using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
public class ProductionUIValidationResults : ICallbacks
{
    private static readonly TestRunnerApi api;
    static ProductionUIValidationResults()
    {
        api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new ProductionUIValidationResults());
    }
    public void RunFinished(ITestResultAdaptor result)
    {
        TestRunnerApi.SaveResultToFile(result, ".utmp/production-node-ui-check/results.xml");
    }
    public void RunStarted(ITestAdaptor test) { }
    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
}
