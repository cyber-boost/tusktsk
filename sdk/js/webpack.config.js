const path = require('path');
const JavaScriptObfuscator = require('webpack-obfuscator');
const TerserPlugin = require('terser-webpack-plugin');
const LicenseWebpackPlugin = require('./license-webpack-plugin');

module.exports = {
  entry: {
    'tsk-protected': './src/tsk-protected.js',
    'tsk-protected.min': './src/tsk-protected.js'
  },
  
  output: {
    path: path.resolve(__dirname, 'dist'),
    filename: '[name].js',
    library: 'TuskLang',
    libraryTarget: 'umd',
    globalObject: 'this'
  },
  
  mode: 'production',
  
  module: {
    rules: [
      {
        test: /\.js$/,
        exclude: /node_modules/,
        use: {
          loader: 'babel-loader',
          options: {
            presets: ['@babel/preset-env'],
            plugins: [
              ['@babel/plugin-transform-runtime', { regenerator: true }]
            ]
          }
        }
      }
    ]
  },
  
  optimization: {
    minimize: true,
    minimizer: [
      new TerserPlugin({
        terserOptions: {
          compress: {
            drop_console: true,
            drop_debugger: true,
            pure_funcs: ['console.log', 'console.info', 'console.debug']
          },
          mangle: {
            reserved: ['TuskLang', 'tsk', 'parse', 'compile', 'validate']
          }
        }
      })
    ]
  },
  
  plugins: [
    // License validation plugin
    new LicenseWebpackPlugin({
      licenseKey: process.env.TUSKLANG_LICENSE,
      apiEndpoint: 'https://lic.tusklang.org/api/v1'
    }),
    
    // JavaScript obfuscator
    new JavaScriptObfuscator({
      compact: true,
      controlFlowFlattening: true,
      controlFlowFlatteningThreshold: 0.75,
      deadCodeInjection: true,
      deadCodeInjectionThreshold: 0.4,
      debugProtection: true,
      debugProtectionInterval: true,
      disableConsoleOutput: true,
      identifierNamesGenerator: 'hexadecimal',
      log: false,
      numbersToExpressions: true,
      renameGlobals: false,
      selfDefending: true,
      simplify: true,
      splitStrings: true,
      splitStringsChunkLength: 10,
      stringArray: true,
      stringArrayEncoding: ['base64'],
      stringArrayThreshold: 0.75,
      transformObjectKeys: true,
      unicodeEscapeSequence: false
    })
  ],
  
  resolve: {
    extensions: ['.js', '.json']
  },
  
  devtool: false, // Disable source maps for protection
}; 