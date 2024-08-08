#! /usr/bin/env sh

head -n 1 src/Xiangyao/Xiangyao.csproj | cat > src/Xiangyao/Xiangyao.csproj.1
echo "<PropertyGroup><PublishAot>true</PublishAot></PropertyGroup>" >> src/Xiangyao/Xiangyao.csproj.1
tail -n +1 src/Xiangyao/Xiangyao.csproj | cat >> src/Xiangyao/Xiangyao.csproj.1
mv src/Xiangyao/Xiangyao.csproj.1 src/Xiangyao/Xiangyao.csproj
