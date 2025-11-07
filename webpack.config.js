const path = require('path');

module.exports = {
    entry: './src/Agent.UI/App.fs.js',
    output: {
        path: path.join(__dirname, './src/Agent.Web/wwwroot'),
        filename: 'bundle.js',
    },
    devServer: {
        static: {
            directory: path.join(__dirname, './src/Agent.Web/wwwroot'),
        },
        port: 8080,
        hot: true,
        proxy: {
            '/api': {
                target: 'http://localhost:5000',
                changeOrigin: true
            }
        }
    },
    module: {
        rules: [
            {
                test: /\.fs(x|proj)?$/,
                use: {
                    loader: 'fable-loader',
                    options: {
                        babel: {
                            presets: ['@babel/preset-env', '@babel/preset-react']
                        }
                    }
                }
            }
        ]
    },
    resolve: {
        extensions: ['.js', '.fs']
    }
};
