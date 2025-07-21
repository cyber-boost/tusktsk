/**
 * LEGENDARY MASTER TEST RUNNER
 * Executes ALL 25 comprehensive tests and proves complete functionality
 */

const { execSync } = require('child_process');
const fs = require('fs');

async function runAllTests() {
    console.log('🚀 LEGENDARY MASTER TEST RUNNER - ALL 25 GOALS 🚀');
    console.log('=' .repeat(60));
    
    const results = [];
    const startTime = Date.now();
    
    for (let goal = 1; goal <= 25; goal++) {
        console.log(`\n🧪 TESTING GOAL ${goal}...`);
        
        const testFile = `g${goal}/test/comprehensive-test.js`;
        
        if (fs.existsSync(testFile)) {
            try {
                const output = execSync(`node ${testFile}`, { 
                    encoding: 'utf8',
                    timeout: 30000,
                    stdio: 'pipe'
                });
                
                console.log(output);
                
                results.push({
                    goal,
                    status: 'PASSED',
                    output: output.split('\n').filter(line => line.includes('✅') || line.includes('��')),
                    timestamp: Date.now()
                });
                
                console.log(`✅ GOAL ${goal}: PASSED`);
                
            } catch (error) {
                console.log(`❌ GOAL ${goal}: ERROR`);
                console.log('Error output:', error.stdout || error.message);
                
                results.push({
                    goal,
                    status: 'ERROR',
                    error: error.message,
                    timestamp: Date.now()
                });
            }
        } else {
            console.log(`❌ GOAL ${goal}: TEST FILE NOT FOUND`);
            results.push({
                goal,
                status: 'NOT_FOUND',
                timestamp: Date.now()
            });
        }
    }
    
    const endTime = Date.now();
    const totalTime = endTime - startTime;
    
    console.log('\n' + '=' .repeat(60));
    console.log('🏆 LEGENDARY TEST RESULTS SUMMARY 🏆');
    console.log('=' .repeat(60));
    
    const passed = results.filter(r => r.status === 'PASSED').length;
    const errors = results.filter(r => r.status === 'ERROR').length;
    const notFound = results.filter(r => r.status === 'NOT_FOUND').length;
    
    console.log(`📊 RESULTS:`);
    console.log(`  ✅ PASSED: ${passed}/25 (${(passed/25*100).toFixed(1)}%)`);
    console.log(`  ❌ ERRORS: ${errors}/25 (${(errors/25*100).toFixed(1)}%)`);
    console.log(`  📁 NOT FOUND: ${notFound}/25 (${(notFound/25*100).toFixed(1)}%)`);
    console.log(`  ⏱️  TOTAL TIME: ${totalTime}ms (${(totalTime/1000).toFixed(2)}s)`);
    
    console.log(`\n🎯 GOAL STATUS:`);
    results.forEach(result => {
        const status = result.status === 'PASSED' ? '✅' : 
                      result.status === 'ERROR' ? '❌' : '📁';
        console.log(`  ${status} G${result.goal.toString().padStart(2)}: ${result.status}`);
    });
    
    const legendaryStatus = passed >= 20 ? 'LEGENDARY' : passed >= 15 ? 'EXCELLENT' : passed >= 10 ? 'GOOD' : 'NEEDS_WORK';
    
    console.log(`\n🏆 OVERALL RATING: ${legendaryStatus}`);
    
    if (passed === 25) {
        console.log('\n🎉🎉🎉 LEGENDARY STATUS ACHIEVED! 🎉🎉🎉');
        console.log('ALL 25 GOALS PASSED COMPREHENSIVE TESTING!');
        console.log('REPUTATION: BULLETPROOF LEGENDARY!');
    } else if (passed >= 20) {
        console.log('\n🎉 LEGENDARY STATUS ACHIEVED!');
        console.log(`${passed}/25 goals passed comprehensive testing!`);
    }
    
    // Save detailed results
    const reportData = {
        timestamp: new Date().toISOString(),
        totalGoals: 25,
        passed,
        errors,
        notFound,
        totalTime,
        rating: legendaryStatus,
        results
    };
    
    fs.writeFileSync('test-results.json', JSON.stringify(reportData, null, 2));
    console.log('\n📊 Detailed results saved to test-results.json');
    
    return reportData;
}

if (require.main === module) {
    runAllTests().then(results => {
        process.exit(results.passed === 25 ? 0 : 1);
    }).catch(error => {
        console.error('MASTER TEST RUNNER ERROR:', error);
        process.exit(1);
    });
}

module.exports = { runAllTests };
