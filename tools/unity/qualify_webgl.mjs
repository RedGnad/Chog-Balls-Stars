// Local Chrome/WebGL smoke; Node 22+, temporary isolated profile, no performance benchmark.
import {spawn} from 'node:child_process';
import {mkdir,mkdtemp,readFile,writeFile,rm} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
const out=process.env.CHOG_BROWSER_OUTPUT_DIR||join(process.cwd(),'.artifacts/fusion');
const url=process.env.CHOG_BROWSER_URL||'http://127.0.0.1:8080';
await mkdir(out,{recursive:true});
const touch=process.env.CHOG_BROWSER_TOUCH==='1';
const artifact=name=>join(out,(touch?'touch-':'')+name);
// CHOG_BROWSER_PROFILE=<folder> keeps what the page stores (its settings, a stake waiting for its
// match to be judged) from one run to the next; without it each run starts from nothing.
const keptProfile=process.env.CHOG_BROWSER_PROFILE||'';
if(keptProfile){
  await mkdir(keptProfile,{recursive:true});
  // The port of the run before would be read in place of this run's.
  await rm(join(keptProfile,'DevToolsActivePort'),{force:true});
}
const profile=keptProfile||await mkdtemp(join(tmpdir(),'chog-browser-'));
const chrome=spawn('/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',[
  '--headless=new','--mute-audio',`--user-data-dir=${profile}`,'--remote-debugging-port=0','--no-first-run',
  // CHOG_BROWSER_GPU=1 draws with the Mac's GPU (Metal) instead of SwiftShader: software drawing
  // runs the game at about five frames a second, too slow to stand for a player.
  '--no-default-browser-check',...(process.env.CHOG_BROWSER_GPU==='1'?['--use-angle=metal','--enable-gpu']:['--enable-unsafe-swiftshader','--use-angle=swiftshader']),
  '--autoplay-policy=no-user-gesture-required','--disable-background-networking','--disable-component-update',
  // CHOG_BROWSER_IGNORE_CERT=1 accepts a self-signed certificate, for a local HTTPS test only.
  ...(process.env.CHOG_BROWSER_IGNORE_CERT==='1'?['--ignore-certificate-errors']:[]),'about:blank'
],{stdio:'ignore'});
const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));
const pending=new Map(),events=[],requestUrls=new Map();
let socket,serial=0,onBinding=null,onFrame=null,extraHeader=null;
const command=(method,params={})=>new Promise((resolve,reject)=>{
  const id=++serial;
  const timer=setTimeout(()=>{pending.delete(id);reject(new Error(`CDP timeout: ${method}`));},30000);
  pending.set(id,{resolve:value=>{clearTimeout(timer);resolve(value);},reject:error=>{clearTimeout(timer);reject(error);}});
  socket.send(JSON.stringify({id,method,params}));
});
const screenshot=async name=>{
  const result=await command('Page.captureScreenshot',{format:'png'});
  await writeFile(artifact(name),Buffer.from(result.data,'base64'));
};
try {
  const deadline=Date.now()+180000;
  let port;
  while(Date.now()<deadline){
    try{port=Number((await readFile(join(profile,'DevToolsActivePort'),'utf8')).split('\n')[0]);break;}
    catch{await sleep(300);}
  }
  if(!port)throw new Error('Chrome local DevTools unavailable');
  const targets=await(await fetch(`http://127.0.0.1:${port}/json/list`)).json();
  socket=new WebSocket(targets.find(t=>t.type==='page').webSocketDebuggerUrl);
  await new Promise((resolve,reject)=>{socket.onopen=resolve;socket.onerror=reject;});
  socket.onmessage=({data})=>{
    const msg=JSON.parse(data);
    if(msg.id&&pending.has(msg.id)){
      const waiter=pending.get(msg.id);pending.delete(msg.id);
      msg.error?waiter.reject(new Error(JSON.stringify(msg.error))):waiter.resolve(msg.result);
    }else if(['Runtime.consoleAPICalled','Runtime.exceptionThrown','Log.entryAdded','Network.loadingFailed'].includes(msg.method)){
      // A failed request is recorded with its address, so the report says what failed.
      if(msg.method==='Network.loadingFailed')msg.params.url=requestUrls.get(msg.params.requestId);
      events.push(msg);
    }else if(msg.method==='Network.requestWillBeSent')requestUrls.set(msg.params.requestId,msg.params.request.url);
    else if(msg.method==='Runtime.bindingCalled'&&onBinding)onBinding(msg.params).catch(error=>console.error(error.message));
    else if(msg.method==='Page.screencastFrame'&&onFrame)onFrame(msg.params);
    else if(msg.method==='Fetch.requestPaused'&&extraHeader){
      // A request to the page's own address goes on with the extra header added.
      const headers=Object.entries(msg.params.request.headers).map(([name,value])=>({name,value}));
      command('Fetch.continueRequest',{requestId:msg.params.requestId,headers:[...headers,extraHeader]}).catch(()=>{});
    }
  };
  const version=await command('Browser.getVersion');
  for(const domain of ['Page','Runtime','Log','Network'])await command(domain+'.enable');
  await command('Emulation.setDeviceMetricsOverride',{width:touch?960:1280,height:touch?540:720,deviceScaleFactor:1,mobile:touch});
  if(touch){
    await command('Emulation.setTouchEmulationEnabled',{enabled:true,maxTouchPoints:2});
    await command('Emulation.setUserAgentOverride',{userAgent:'Mozilla/5.0 (Linux; Android 11; ChogBrowserTest) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Mobile Safari/537.36'});
  }
  // CHOG_BROWSER_EXTRA_HEADER="name: value" is added to every request sent to the address of
  // the page, and to no other: a header a browser does not know makes a request to another
  // site fail (seen with the wallet connector's script). Behind a free ngrok tunnel,
  // "ngrok-skip-browser-warning: 1" skips the warning page ngrok puts in front of a browser's
  // first visit.
  if(process.env.CHOG_BROWSER_EXTRA_HEADER){
    const [name,...rest]=process.env.CHOG_BROWSER_EXTRA_HEADER.split(':');
    extraHeader={name:name.trim(),value:rest.join(':').trim()};
    await command('Fetch.enable',{patterns:[{urlPattern:new URL(url).origin+'/*'}]});
  }
  if(process.env.CHOG_BROWSER_WALLET||process.env.CHOG_BROWSER_WALLET_KEY){
    // A stand-in for the wallet a browser provides (window.ethereum), so the wallet path of the
    // game can be run here. It shares its address once asked with eth_requestAccounts, as a
    // wallet does on a first visit.
    // CHOG_BROWSER_WALLET=<address> shares that address and signs nothing: it proves no control
    //   of it. CHOG_BROWSER_WALLET=refuse answers as a player who closes the wallet (code 4001).
    // CHOG_BROWSER_WALLET_KEY=<private key in hex> is a real key-pair wallet: its address comes
    //   from the key and personal_sign really signs. Only ever give it a throwaway test key.
    // CHOG_BROWSER_WALLET_RPC=<endpoint of a local anvil> also lets it read the chain and send
    //   transactions there, as a wallet does for a purchase: every transaction is accepted at
    //   once and sent from the wallet's address, which anvil is asked to impersonate. Only for a
    //   local copy of a chain: nothing here can sign a transaction for a real one.
    // CHOG_BROWSER_WALLET_SIGN=1, with a key and an endpoint, signs each transaction with the key
    //   and sends it (tools/unity/test_wallet_tx.mjs) instead of asking anvil to impersonate: for
    //   Monad's testnet, with a throwaway key made for the test and some testnet MON on it.
    let address=process.env.CHOG_BROWSER_WALLET,sign=null;
    const walletRpc=process.env.CHOG_BROWSER_WALLET_RPC||'';
    const signsTransactions=process.env.CHOG_BROWSER_WALLET_SIGN==='1'&&Boolean(process.env.CHOG_BROWSER_WALLET_KEY);
    const bindings={};
    onBinding=async({name,payload})=>{if(bindings[name])await bindings[name](payload);};
    if(walletRpc){
      const ask=async(method,params)=>(await(await fetch(walletRpc,{method:'POST',headers:{'Content-Type':'application/json'},
        body:JSON.stringify({jsonrpc:'2.0',id:1,method,params})})).json());
      const askResult=async(method,params)=>{const answer=await ask(method,params);if(answer.error)throw new Error(answer.error.message||method+' failed');return answer.result;};
      const signer=signsTransactions?await import(new URL('./test_wallet_tx.mjs',import.meta.url)):null;
      const signingKey=signsTransactions?Uint8Array.from(process.env.CHOG_BROWSER_WALLET_KEY.replace(/^0x/,'').match(/.{2}/g),pair=>parseInt(pair,16)):null;
      await command('Runtime.addBinding',{name:'chogTestRpc'});
      bindings.chogTestRpc=async payload=>{
        const {id,method,params}=JSON.parse(payload);
        let answer;
        try{
          if(method==='eth_sendTransaction'&&signer){
            answer={result:await signer.sendTransaction(params[0],signingKey,askResult)};
            console.log('stand-in wallet sent '+answer.result);
          }else{
            if(method==='eth_sendTransaction')await ask('anvil_impersonateAccount',[params[0].from]);
            answer=await ask(method,params??[]);
          }
        }catch(error){answer={error:{message:String(error.message||error)}};}
        await command('Runtime.evaluate',{expression:`window.chogTestAnswered(${Number(id)},${JSON.stringify(JSON.stringify(answer))})`});
      };
    }
    if(process.env.CHOG_BROWSER_WALLET_KEY){
      const modules=new URL('../../server/node_modules/@noble/',import.meta.url);
      const {secp256k1}=await import(new URL('curves/secp256k1.js',modules));
      const {keccak_256}=await import(new URL('hashes/sha3.js',modules));
      const toHex=bytes=>Array.from(bytes,byte=>byte.toString(16).padStart(2,'0')).join('');
      const fromHex=hex=>Uint8Array.from(hex.replace(/^0x/,'').match(/.{2}/g)??[],pair=>parseInt(pair,16));
      const secretKey=fromHex(process.env.CHOG_BROWSER_WALLET_KEY);
      address='0x'+toHex(keccak_256(secp256k1.getPublicKey(secretKey,false).slice(1)).slice(-20));
      sign=hex=>{
        const text=fromHex(hex),prefix=new TextEncoder().encode(`\x19Ethereum Signed Message:\n${text.length}`);
        const signature=secp256k1.sign(keccak_256(Uint8Array.from([...prefix,...text])),secretKey,{prehash:false,format:'recovered'});
        return '0x'+toHex(signature.slice(1))+(27+signature[0]).toString(16);
      };
      await command('Runtime.addBinding',{name:'chogTestSign'});
      bindings.chogTestSign=async payload=>{
        const {id,hex}=JSON.parse(payload);
        await command('Runtime.evaluate',{expression:`window.chogTestSigned(${Number(id)},${JSON.stringify(sign(hex))})`});
      };
    }
    // Like a wallet, it remembers within the browser session that it shared its address.
    await command('Page.addScriptToEvaluateOnNewDocument',{source:`(()=>{
      const remembered=()=>{try{return sessionStorage.getItem('chogTestShared')==='1';}catch(e){return false;}};
      const remember=()=>{try{sessionStorage.setItem('chogTestShared','1');}catch(e){}};
      let shared=remembered(),calls=0;const waiting={};
      window.chogTestSigned=(id,signature)=>{waiting[id](signature);delete waiting[id];};
      window.chogTestAnswered=(id,text)=>{waiting[id](JSON.parse(text));delete waiting[id];};
      const onChain=['eth_chainId','eth_call','eth_sendTransaction','eth_getTransactionReceipt','eth_blockNumber'];
      window.ethereum={isChogTestStandIn:true,on(){},request:async({method,params})=>{
        if(${walletRpc?'true':'false'}&&(method==='wallet_switchEthereumChain'||method==='wallet_addEthereumChain')){
          console.log('CHOG_TEST_WALLET '+method+' '+JSON.stringify(params));return null;
        }
        if(${walletRpc?'true':'false'}&&onChain.includes(method)){
          const answer=await new Promise(resolve=>{const id=++calls;waiting[id]=resolve;window.chogTestRpc(JSON.stringify({id,method,params}));});
          if(answer.error)throw Object.assign(new Error(answer.error.message||'the chain refused'),{code:answer.error.code||-32000});
          return answer.result;
        }
        if(method==='eth_accounts')return shared?[${JSON.stringify(address)}]:[];
        if(method==='eth_requestAccounts'){
          if(${JSON.stringify(address)}==='refuse')throw Object.assign(new Error('User rejected the request.'),{code:4001});
          shared=true;remember();return [${JSON.stringify(address)}];
        }
        if(method==='personal_sign'&&${sign?'true':'false'}){
          return new Promise(resolve=>{const id=++calls;waiting[id]=resolve;window.chogTestSign(JSON.stringify({id,hex:params[0]}));});
        }
        throw Object.assign(new Error('The test stand-in does not answer '+method),{code:4200});
      }};
    })();`});
  }
  await command('Page.navigate',{url});
  let loaded=false;
  while(Date.now()<deadline){
    await sleep(1000);
    const state=await command('Runtime.evaluate',{expression:"JSON.stringify({loading:(document.querySelector('#unity-loading-container')||document.querySelector('#unity-loading-bar'))?.style.display,banner:document.querySelector('#unity-warning')?.innerText,canvas:!!document.querySelector('#unity-canvas')})",returnByValue:true});
    const value=JSON.parse(state.result.value||'{}');
    if(value.loading==='none'&&value.canvas){loaded=true;break;}
    if(value.banner?.trim())throw new Error('Unity banner: '+value.banner.trim());
  }
  if(!loaded)throw new Error('Unity loader timeout');
  await sleep(Number(process.env.CHOG_BROWSER_SETTLE_MS||1800));await screenshot('browser-menu.png');
  // A tap of the menu: a finger on a touch page (the game picks its controls from what was last
  // used, ControlModeWatcher), the left mouse button on a desktop one.
  const tap=async(x,y)=>{
    if(touch){
      await command('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{id:3,x,y}]});
      await sleep(60);
      await command('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
    }else{
      await command('Input.dispatchMouseEvent',{type:'mousePressed',x,y,button:'left',buttons:1,clickCount:1});
      await command('Input.dispatchMouseEvent',{type:'mouseReleased',x,y,button:'left',buttons:0,clickCount:1});
    }
  };
  if(process.env.CHOG_BROWSER_CLICK_X&&process.env.CHOG_BROWSER_CLICK_Y){
    const x=Number(process.env.CHOG_BROWSER_CLICK_X),y=Number(process.env.CHOG_BROWSER_CLICK_Y);
    await tap(x,y);
    await sleep(Number(process.env.CHOG_BROWSER_AFTER_CLICK_MS||10000));
    await screenshot('browser-after-click.png');
  }
  if(process.env.CHOG_BROWSER_CLICK_2_X&&process.env.CHOG_BROWSER_CLICK_2_Y){
    const x=Number(process.env.CHOG_BROWSER_CLICK_2_X),y=Number(process.env.CHOG_BROWSER_CLICK_2_Y);
    await tap(x,y);
    await sleep(Number(process.env.CHOG_BROWSER_AFTER_CLICK_2_MS||10000));
    await screenshot('browser-after-second-click.png');
  }
  if(process.env.CHOG_BROWSER_CLICK_3_X&&process.env.CHOG_BROWSER_CLICK_3_Y){
    const x=Number(process.env.CHOG_BROWSER_CLICK_3_X),y=Number(process.env.CHOG_BROWSER_CLICK_3_Y);
    await tap(x,y);
    await sleep(Number(process.env.CHOG_BROWSER_AFTER_CLICK_3_MS||10000));
    await screenshot('browser-after-third-click.png');
  }
  if(process.env.CHOG_BROWSER_CLICK_4_X&&process.env.CHOG_BROWSER_CLICK_4_Y){
    const x=Number(process.env.CHOG_BROWSER_CLICK_4_X),y=Number(process.env.CHOG_BROWSER_CLICK_4_Y);
    await tap(x,y);
    await sleep(Number(process.env.CHOG_BROWSER_AFTER_CLICK_4_MS||10000));
    await screenshot('browser-after-fourth-click.png');
  }
  // Longer paths: clicks 5 to 9, each with its own wait and a screenshot named after its number.
  for(let number=5;number<=9;number++){
    const x=Number(process.env[`CHOG_BROWSER_CLICK_${number}_X`]),y=Number(process.env[`CHOG_BROWSER_CLICK_${number}_Y`]);
    if(!process.env[`CHOG_BROWSER_CLICK_${number}_X`]||!process.env[`CHOG_BROWSER_CLICK_${number}_Y`])continue;
    await tap(x,y);
    await sleep(Number(process.env[`CHOG_BROWSER_AFTER_CLICK_${number}_MS`]||10000));
    await screenshot(`browser-after-click-${number}.png`);
  }
  if(process.env.CHOG_BROWSER_HOLD_KEY){
    // Hold one movement key (w, a, s or d) after the clicks, then capture where the character went.
    const key=process.env.CHOG_BROWSER_HOLD_KEY.toLowerCase(),code='Key'+key.toUpperCase(),vk=key.toUpperCase().charCodeAt(0);
    await command('Input.dispatchKeyEvent',{type:'keyDown',key,code,windowsVirtualKeyCode:vk,nativeVirtualKeyCode:vk});
    await sleep(Number(process.env.CHOG_BROWSER_HOLD_MS||2000));
    await command('Input.dispatchKeyEvent',{type:'keyUp',key,code,windowsVirtualKeyCode:vk,nativeVirtualKeyCode:vk});
    await screenshot('browser-after-key.png');
  }
  if(process.env.CHOG_BROWSER_DRAG){
    // "x1,y1,x2,y2": press at the first point, drag to the second and hold, e.g. to push the on-screen stick.
    // With CHOG_BROWSER_TOUCH=1 it is a finger (at 960x540 the move stick is centred near 120,433).
    const [x1,y1,x2,y2]=process.env.CHOG_BROWSER_DRAG.split(',').map(Number);
    if(touch){
      await command('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{id:5,x:x1,y:y1}]});
      await command('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{id:5,x:(x1+x2)/2,y:(y1+y2)/2}]});
      await command('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{id:5,x:x2,y:y2}]});
      await sleep(Number(process.env.CHOG_BROWSER_DRAG_MS||2000));
      await screenshot('browser-during-drag.png');
      await command('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
    }else{
      await command('Input.dispatchMouseEvent',{type:'mousePressed',x:x1,y:y1,button:'left',buttons:1,clickCount:1});
      await command('Input.dispatchMouseEvent',{type:'mouseMoved',x:(x1+x2)/2,y:(y1+y2)/2,button:'left',buttons:1});
      await command('Input.dispatchMouseEvent',{type:'mouseMoved',x:x2,y:y2,button:'left',buttons:1});
      await sleep(Number(process.env.CHOG_BROWSER_DRAG_MS||2000));
      await screenshot('browser-during-drag.png');
      await command('Input.dispatchMouseEvent',{type:'mouseReleased',x:x2,y:y2,button:'left',buttons:0,clickCount:1});
    }
  }
  if(process.env.CHOG_BROWSER_SPECIAL){
    // "x,y": on a desktop page the special is the right mouse button, aimed at the pointer. Hold
    // it on that point, release, and capture the attack 150, 400 and 900 ms later. Needs a
    // charged special (page option "charged=1"). The left button aims the basic attack, also
    // when it is pressed on the on-screen special stick.
    // With CHOG_BROWSER_TOUCH=1 give "x1,y1,x2,y2": a finger presses the special stick at the
    // first point and drags to the second to aim (at 960x540 the stick is centred near 692,428).
    const numbers=process.env.CHOG_BROWSER_SPECIAL.split(',').map(Number);
    // CHOG_BROWSER_SPECIAL_FILM=<milliseconds> keeps every frame the page draws for that long
    // after the release, instead of the fixed screenshots: a screenshot takes several hundred
    // milliseconds here, too slow to follow an attack that moves.
    const filmFor=Number(process.env.CHOG_BROWSER_SPECIAL_FILM||0),film=[];
    // The film starts just before the release: started before the press, the press itself was
    // seen to wait for tens of seconds when the hero had walked first.
    const startFilm=async()=>{
      if(!filmFor)return;
      onFrame=frame=>{
        film.push({data:frame.data,at:frame.metadata.timestamp*1000});
        command('Page.screencastFrameAck',{sessionId:frame.sessionId}).catch(()=>{});
      };
      await command('Page.startScreencast',{format:'jpeg',quality:80,everyNthFrame:1});
    };
    if(touch){
      const [x1,y1,x2,y2]=numbers;
      await command('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{id:7,x:x1,y:y1}]});
      await command('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{id:7,x:(x1+x2)/2,y:(y1+y2)/2}]});
      await command('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{id:7,x:x2,y:y2}]});
      await sleep(350);await screenshot('browser-special-aim.png');
      await startFilm();
      await command('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
      if(!filmFor)await screenshot('browser-special-0ms.png');
    }else{
      const [x,y]=numbers;
      await command('Input.dispatchMouseEvent',{type:'mouseMoved',x,y,button:'none',buttons:0});
      await command('Input.dispatchMouseEvent',{type:'mousePressed',x,y,button:'right',buttons:2,clickCount:1});
      await sleep(350);await screenshot('browser-special-aim.png');
      await startFilm();
      await command('Input.dispatchMouseEvent',{type:'mouseReleased',x,y,button:'right',buttons:0,clickCount:1});
    }
    // CHOG_BROWSER_SPECIAL_SHOTS="150,300,450" moves those instants (milliseconds after release).
    const released=Date.now();
    if(filmFor>0){
      await sleep(filmFor);
      await command('Page.stopScreencast');onFrame=null;
      let number=0;
      for(const frame of film){
        const after=Math.round(frame.at-released);
        if(after<0)continue;
        number++;
        await writeFile(artifact(`browser-special-film-${String(number).padStart(2,'0')}-${after}ms.jpg`),Buffer.from(frame.data,'base64'));
      }
      console.log(`FILM ${number} frames kept of ${film.length}`);
    }
    const instants=filmFor>0?[]:(process.env.CHOG_BROWSER_SPECIAL_SHOTS||'150,400,900').split(',').map(Number);
    for(const instant of instants){
      await sleep(Math.max(0,released+instant-Date.now()));
      await screenshot(`browser-special-${instant}ms.png`);
    }
  }
  if(process.env.CHOG_BROWSER_RELOAD==='1'){
    // Open the page again in the same browser, as a player who comes back: what the game kept
    // (the chosen Genesis, the sign-in) should be there without asking again.
    await sleep(2500);
    await command('Page.navigate',{url});
    let back=false;
    while(Date.now()<deadline){
      await sleep(1000);
      const state=await command('Runtime.evaluate',{expression:"JSON.stringify({loading:(document.querySelector('#unity-loading-container')||document.querySelector('#unity-loading-bar'))?.style.display,canvas:!!document.querySelector('#unity-canvas')})",returnByValue:true}).catch(()=>null);
      const value=JSON.parse((state&&state.result&&state.result.value)||'{}');
      if(value.loading==='none'&&value.canvas){back=true;break;}
    }
    if(!back)throw new Error('Unity loader timeout after the reload');
    await sleep(Number(process.env.CHOG_BROWSER_SETTLE_MS||1800)+4000);await screenshot('browser-after-reload.png');
  }
  if(process.env.CHOG_BROWSER_REPORT_LOCATION==='1'){
    // Where the page is after the scripted taps, when one of them is expected to leave it.
    const where=await command('Runtime.evaluate',{expression:'location.href',returnByValue:true}).catch(()=>null);
    console.log('LOCATION '+(where&&where.result?where.result.value:'unknown'));
  }
  if(process.env.CHOG_BROWSER_EVALUATE){
    // Prints what a page expression is worth after the scripted taps, e.g. window.chogLastLink.
    const value=await command('Runtime.evaluate',{expression:process.env.CHOG_BROWSER_EVALUATE,returnByValue:true}).catch(()=>null);
    console.log('EVALUATE '+JSON.stringify(value&&value.result?value.result.value:null));
  }
  if(process.env.CHOG_BROWSER_LINGER_MS){
    // Keep the match running after the scripted input, so the console collects more of it.
    // With CHOG_BROWSER_WAIT_FOR the wait ends as soon as a console line contains that text.
    // CHOG_BROWSER_LINGER_SHOTS=n also takes n screenshots on the way, evenly spaced.
    const started=Date.now(),lasts=Number(process.env.CHOG_BROWSER_LINGER_MS);
    const until=started+lasts,wanted=process.env.CHOG_BROWSER_WAIT_FOR;
    const shots=Number(process.env.CHOG_BROWSER_LINGER_SHOTS||0);
    let taken=0;
    const seen=()=>wanted&&events.some(event=>event.method==='Runtime.consoleAPICalled'
      &&event.params.args.some(argument=>String(argument.value??'').includes(wanted)));
    while(Date.now()<until&&!seen()){
      await sleep(1000);
      if(taken<shots&&Date.now()-started>=(taken+1)*lasts/(shots+1)){taken++;await screenshot(`browser-linger-${taken}.png`);}
    }
    await sleep(1500);await screenshot('browser-linger.png');
  }
  if(process.env.CHOG_BROWSER_TRAINING==='1'){
    const result=await command('Runtime.evaluate',{expression:"JSON.stringify(document.querySelector('#unity-canvas').getBoundingClientRect().toJSON())",returnByValue:true});
    const rect=JSON.parse(result.result.value),scale=Math.sqrt((rect.width/1280)*(rect.height/720));
    const x=rect.x+270*scale,y=rect.y+240*scale;
    await command('Input.dispatchMouseEvent',{type:'mousePressed',x,y,button:'left',buttons:1,clickCount:1});
    await command('Input.dispatchMouseEvent',{type:'mouseReleased',x,y,button:'left',buttons:0,clickCount:1});
    await sleep(12000);await screenshot('browser-training.png');
    if(touch){
      const left={id:1,x:rect.x+150*scale,y:rect.y+rect.height-140*scale};
      const right={id:2,x:rect.x+rect.width-90*scale,y:rect.y+rect.height-140*scale};
      await command('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[left,right]});
      await sleep(1200);await screenshot('browser-aim.png');
      await command('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
      await sleep(500);await screenshot('browser-action.png');
      await sleep(900);await screenshot('browser-post-primary.png');
      const special={id:3,x:rect.x+rect.width-300*scale,y:rect.y+rect.height-260*scale};
      await command('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[special]});
      await sleep(250);await screenshot('browser-special-aim.png');
      await command('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
      await sleep(2400);await screenshot('browser-special.png');
    }else{
      await command('Input.dispatchKeyEvent',{type:'keyDown',key:'d',code:'KeyD',windowsVirtualKeyCode:68,nativeVirtualKeyCode:68});
      await sleep(700);
      await command('Input.dispatchKeyEvent',{type:'keyUp',key:'d',code:'KeyD',windowsVirtualKeyCode:68,nativeVirtualKeyCode:68});
      await command('Input.dispatchMouseEvent',{type:'mouseMoved',x:rect.x+rect.width*.75,y:rect.y+rect.height*.5,button:'none',buttons:0});
      await command('Input.dispatchMouseEvent',{type:'mousePressed',x:rect.x+rect.width*.75,y:rect.y+rect.height*.5,button:'left',buttons:1,clickCount:1});
      await sleep(500);await screenshot('browser-aim.png');
      await command('Input.dispatchMouseEvent',{type:'mouseReleased',x:rect.x+rect.width*.75,y:rect.y+rect.height*.5,button:'left',buttons:0,clickCount:1});
      await sleep(500);await screenshot('browser-action.png');
      await sleep(900);await screenshot('browser-post-primary.png');
      await command('Input.dispatchMouseEvent',{type:'mousePressed',x:rect.x+rect.width*.75,y:rect.y+rect.height*.5,button:'right',buttons:2,clickCount:1});
      await sleep(350);await screenshot('browser-special-aim.png');
      await command('Input.dispatchMouseEvent',{type:'mouseReleased',x:rect.x+rect.width*.75,y:rect.y+rect.height*.5,button:'right',buttons:0,clickCount:1});
      await sleep(2400);await screenshot('browser-special.png');
    }
  }
  await writeFile(artifact('browser-smoke-result.json'),JSON.stringify({recordedAtUtc:new Date().toISOString(),browser:version.product,renderer:'SwiftShader software WebGL',result:'Unity loaded; screenshots require visual inspection',phoneTested:false,touchEmulated:touch,inputAttempted:process.env.CHOG_BROWSER_TRAINING==='1'},null,2)+'\n');
  console.log('Unity loaded. Browser screenshots saved.');
}catch(error){
  await writeFile(artifact('browser-smoke-result.json'),JSON.stringify({recordedAtUtc:new Date().toISOString(),result:'Failed',error:String(error)},null,2)+'\n');
  console.error(String(error));process.exitCode=1;
}finally{
  await writeFile(artifact('browser-smoke-events.json'),JSON.stringify(events,null,2));
  if(socket)socket.close();
  chrome.kill('SIGTERM');
  await Promise.race([new Promise(resolve=>chrome.once('exit',resolve)),sleep(5000)]);
  if(chrome.exitCode===null&&chrome.signalCode===null)chrome.kill('SIGKILL');
  if(!keptProfile)await rm(profile,{recursive:true,force:true,maxRetries:3,retryDelay:300});
}
