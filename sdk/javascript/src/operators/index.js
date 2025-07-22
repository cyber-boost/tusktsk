/**
 * Cloud Infrastructure Operators Index
 * Exports all cloud infrastructure operators for TuskLang SDK
 */

const { executeAwsOperator } = require('./aws-operator');
const { executeAzureOperator } = require('./azure-operator');
const { executeGcpOperator } = require('./gcp-operator');
const { executeKubernetesOperator } = require('./kubernetes-operator');
const { executeDockerOperator } = require('./docker-operator');
const { executeTerraformOperator } = require('./terraform-operator');

module.exports = {
    executeAwsOperator,
    executeAzureOperator,
    executeGcpOperator,
    executeKubernetesOperator,
    executeDockerOperator,
    executeTerraformOperator
}; 