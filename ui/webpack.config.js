const CopyWebpackPlugin = require('copy-webpack-plugin');
const ExtractTextPlugin = require("extract-text-webpack-plugin");
const WriteFilePlugin = require('write-file-webpack-plugin')
const path = require('path');
const webpack = require("webpack");

let commonConfig = {
  devtool: 'inline-source-map',
  resolve: {
    alias: {
      'vue$': 'vue/dist/vue.esm.js'
    },
    extensions: ['.ts', '.js']
  }
}

let browserRules = [
  {
    test: /\.ts$/,
    loader: 'awesome-typescript-loader'
  },
  {
    test: /\.html$/,
    loader: 'html-loader'
  },
  {
    test: /\.woff(\?v=\d+\.\d+\.\d+)?$/,
    loader: 'url-loader?limit=10000&mimetype=application/font-woff'
  },
  {
    test: /\.woff2(\?v=\d+\.\d+\.\d+)?$/,
    loader: 'url-loader?limit=10000&mimetype=application/font-woff'
  },
  {
    test: /\.ttf(\?v=\d+\.\d+\.\d+)?$/,
    loader: 'url-loader?limit=10000&mimetype=application/octet-stream'
  },
  {
    test: /\.eot(\?v=\d+\.\d+\.\d+)?$/,
    loader: 'file-loader'
  },
  {
    test: /\.svg(\?v=\d+\.\d+\.\d+)?$/,
    loader: 'url-loader?limit=10000&mimetype=image/svg+xml'
  }
]

function HashReplace() {}
HashReplace.prototype.apply = function(compiler) {
  compiler.plugin('emit', function(compilation, callback) {

    let replaceRegexes = new Map([
      ['styles.css', /^styles\.[a-f0-9]+\.css$/],
      ['bundle.js', /^bundle\.[a-f0-9]+\.js$/]
    ])

    if ('../index.html' in compilation.assets){
      let replace = new Map();
      replaceRegexes.forEach(function (regex, fileName){
        compilation.chunks.forEach(function(chunk) {
          chunk.files.forEach(function(emittedFileName) {
            if (emittedFileName.match(regex)){
              replace.set(fileName, emittedFileName)
            }
          })
        })
      })

      let indexAsset = compilation.assets['../index.html']
      let sourceFn = indexAsset.source;
      let newSourceFn = () => {
        let sourceStr = sourceFn().toString('utf8')
        replace.forEach(function (emittedFileName, fileName){
          sourceStr = sourceStr.replace(fileName, emittedFileName)
        })
        return Buffer.from(sourceStr, 'utf8')
      }
      indexAsset.source = newSourceFn
      indexAsset.size = () => newSourceFn().length
    }

    callback()

  })
}

let devConfig = {
  module: {
    rules: [
      ...browserRules,
      {
        test: /\.css$/,
        use: ExtractTextPlugin.extract({
          fallback: 'style-loader',
          use: [ 
            { loader: 'css-loader' } 
          ]
        })
      }
    ],
  },
  entry: {
    app: [
      'webpack-dev-server/client?/',
      'webpack/hot/only-dev-server',
      'es6-shim',
      'whatwg-fetch',
      './src/index',
    ]
  },
  plugins: [
    new webpack.HotModuleReplacementPlugin(),
    new webpack.NamedModulesPlugin(),
    new ExtractTextPlugin("styles.css"),
    new webpack.DefinePlugin({
      'BUILD_DEVENV': JSON.stringify(process.env.DEVENV || 'local'),
      'RUNTIME_ENV': JSON.stringify('browser'),
    })
  ],
  output: {
    filename: 'bundle.js',
    publicPath: '/assets/'
  },
  devServer: {
    host: '0.0.0.0',
    port: 3000,
    historyApiFallback: true,
    inline: false,
    hot: true,
    contentBase: './src/public',
    watchOptions: {
      aggregateTimeout: 300,
      poll: 1000
    },
  }
}

let prodConfig = {
  module: {
    rules: [
      ...browserRules,
      {
        test: /\.css$/,
        use: ExtractTextPlugin.extract({
          fallback: 'style-loader',
          use: [ 
            { loader: 'css-loader', options: { minimize: true } } 
          ]
        })
      }
    ],
  },
  entry: {
    app: [
      'es6-shim',
      'whatwg-fetch',
      './src/index',
    ]
  },
  plugins: [
    new webpack.optimize.UglifyJsPlugin(),
    new ExtractTextPlugin("styles.[contenthash].css"),
    new CopyWebpackPlugin([{
      from: path.resolve(__dirname, 'src', 'public'),
      to: path.resolve(__dirname, 'dist')
    }]),
    new webpack.DefinePlugin({
      'BUILD_DEVENV': JSON.stringify(process.env.DEVENV || 'local'),
      'RUNTIME_ENV': JSON.stringify('browser'),
    }),
    new HashReplace()
  ],
  output: {
    filename: 'bundle.[chunkhash].js',
    publicPath: '/assets/',
    path: path.resolve(__dirname, 'dist', 'assets')
  }
}

if (process.env.NODE_ENV === 'production') {
  module.exports = [
    Object.assign({}, commonConfig, prodConfig),
  ]
} else {
  module.exports = [
    Object.assign({}, commonConfig, devConfig),
  ]
}
