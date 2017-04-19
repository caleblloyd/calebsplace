# Calebs place = [Sephs](https://github.com/josephg/sephsplace) place with a .NET Core backend

[Go check it out](https://place.caleblloyd.com)

## Motivation

After watching @josephg build [josephg/sephsplace](https://github.com/josephg/sephsplace) r/place in a weekend, I decided to take a stab at a .NET Core backend.  I wanted to see if I could implement a web service that leveraged memory to hit the 333 Updates/second and 100,000 concurrent user limit (these numbers came from Reddit's [How We Built r/Place
](https://redditblog.com/2017/04/13/how-we-built-rplace/) blog).  I enjoy writing .NET Core apps for a mix of performance and speed of development, so I gave it a shot.

## Backend

The backend is a .NET Core project.  Pixels are upated in-memory, and persisted to a MySQL database every second.  The 1000x1000 image can be split into 10 shards, allowing for one or more backend to serve each shard.

There is quite a bit of caching going on in the .NET Core app.  I tested it on a desktop with an 4-core I5-6600K and was able to sustain 333 updates/second while having ~40,000 Server Sent Events listening.  It was not maxing CPUs yet, but it was filling my Gigabit link (load testing was done from a separate laptop).

Sharding to 10 beefy servers with a beefy MySQL backend should be able to surpass Reddit's target numbers with ease.

## Frontend

@josephg is a better frontend developer than I'll ever be, so I lifted his entire frontend.

My place uses Server Sent Events instead of web sockets.  I wanted to work entirely with HTTP, so that was my option.

## Development

Like @josephg, I also had 2 days of free time to work on the server.  Good thing I didn't have to build a frontend or it wouldn't have gotten done in time :)  I started with a [.NET Core boiler plate](https://github.com/caleblloyd/dotnet-core-boilerplate) that I have been working on.  The boilerplate includes a Docker Compose setup, which allowed me to get up and running quickly.

## Deployment

Deployment is to a personal Kubernetes cluster that I maintain at Google Compute Engine.  I ended up having to use a "poor man's load balancer" because I needed sticky routes, and that's apparently only available in NGINX plus.  Instead I have a script running that polls the Kubernetes DNS service for container IPs, and rewrites it's configuration in a consistent manner.

As of this writing I have 2 shards running and a dinky MySQL server backing the whole operation.  I added rate limiting in NIGNX, but if things get out of control I may need to beef up my servers.

I plan to keep this online for a few days provided things don't get too out of hand with trolls/bots drawing dumb stuff.

I'll try to write a blog post soon that goes more into the technical decisions behind the backend!
