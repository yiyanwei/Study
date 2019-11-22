import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

//配置自定义模块路由实现懒加载
const routes: Routes = [{
  path: 'user', loadChildren: './Modules/user/user.module#UserModule'
}, {
  path: 'product', loadChildren: './Modules/product/product.module#ProductModule'
},{path:'**',redirectTo:'user'}];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
