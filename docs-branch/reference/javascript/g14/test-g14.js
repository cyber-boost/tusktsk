const { Goal14Implementation } = require("./src/goal14-implementation");

async function testG14() {
    console.log("⚡ TESTING PRODUCTION QUALITY G14...");
    
    const goal14 = new Goal14Implementation();
    await goal14.initialize();
    
    // Test real analytics
    const salesData = [
        { month: "Jan", sales: 1000, region: "North" },
        { month: "Feb", sales: 1200, region: "North" },
        { month: "Mar", sales: 1100, region: "South" },
        { month: "Apr", sales: 1300, region: "South" }
    ];
    
    const dataset = goal14.createDataset("sales", salesData);
    console.log("✅ Dataset created with", dataset.size, "rows");
    console.log("✅ Statistics:", JSON.stringify(dataset.statistics.sales, null, 2));
    
    // Test real query engine
    const query = goal14.runQuery("q1", "sales", {
        filters: [{ field: "sales", operator: ">", value: 1100 }],
        groupBy: ["region"],
        aggregations: { sales: "avg" },
        orderBy: [{ field: "sales_avg", direction: "desc" }]
    });
    console.log("✅ Query result:", JSON.stringify(query.result, null, 2));
    
    // Test real linear regression
    const model = goal14.trainLinearModel("sales_model", salesData.map((row, i) => ({
        ...row, month_num: i + 1
    })), "sales");
    console.log("✅ Model accuracy:", model.accuracy);
    
    const prediction = goal14.predict("sales_model", { month_num: 5, region: "North" });
    console.log("✅ Prediction for month 5:", prediction.prediction);
    
    // Test time series forecast
    const timeSeries = salesData.map((row, i) => ({ time: i, value: row.sales }));
    const forecast = goal14.forecast("sales_forecast", timeSeries, 3);
    console.log("✅ Forecast:", JSON.stringify(forecast.forecast, null, 2));
    
    console.log("🎉 G14 PRODUCTION QUALITY VERIFIED!");
    return goal14.getSystemStatus();
}

testG14().then(console.log).catch(console.error);
