<template>
  <div>
    <header>
      <el-form :inline="true" :model="form" class="demo-form-inline">
        <el-form-item label="供应商名称">
          <el-input v-model="form.likeSupplierName" placeholder="请输入供应商名称"></el-input>
        </el-form-item>
        <el-form-item label="供应商编号">
          <el-input v-model="form.proCode" placeholder="请输入供应商编号"></el-input>
        </el-form-item>
        <el-form-item label="联系人">
          <el-input v-model="form.contactMan" placeholder="请输入联系人"></el-input>
        </el-form-item>
        <el-form-item label="联系电话">
          <el-input v-model="form.contactPhone" placeholder="请输入联系电话"></el-input>
        </el-form-item>
        <el-form-item label="开始时间">
          <el-date-picker v-model="form.startTime" type="datetime" placeholder="请输入开始时间"></el-date-picker>
        </el-form-item>
        <el-form-item label="结束时间">
          <el-date-picker v-model="form.endTime" type="datetime" placeholder="请输入结束时间"></el-date-picker>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="onSearch">查询</el-button>
          <el-button type="primary" @click="dialogAddSupplier = true">新增</el-button>
        </el-form-item>
      </el-form>
    </header>
    <el-table :data="tableData" style="width: 100%">
      <el-table-column type="expand">
        <template slot-scope="props">
          <el-form label-position="left" inline class="demo-table-expand">
            <el-form-item label="供应商名称">
              <span>{{ props.row.supplierName }}</span>
            </el-form-item>
            <el-form-item label="供应商编码">
              <span>{{ props.row.supplierCode }}</span>
            </el-form-item>
            <el-form-item label="联系人">
              <span>{{ props.row.contactMan }}</span>
            </el-form-item>
            <el-form-item label="联系电话">
              <span>{{ props.row.contactPhone }}</span>
            </el-form-item>
            <el-form-item label="创建时间">
              <span>{{ props.row.creationTime }}</span>
            </el-form-item>
            <el-form-item label="营业执照">
              <VueHoverMask>
                <!-- 默认插槽 -->
                <img
                  :src="getThumUrl(props.row.thumbnailImgs)"
                  alt
                  class="el-upload-list__item-thumbnail"
                />
                <template v-slot:action>
                  <i
                    class="el-icon-zoom-in"
                    @click="handlePicturePreview"
                    :data-sourceimgs="props.row.sourceImgs"
                  ></i>
                </template>
              </VueHoverMask>
            </el-form-item>
          </el-form>
        </template>
      </el-table-column>
      <el-table-column label="供应商名称" prop="supplierName"></el-table-column>
      <el-table-column label="供应商编码" prop="supplierCode"></el-table-column>
      <el-table-column label="联系人" prop="contactMan"></el-table-column>
      <el-table-column label="联系电话" prop="contactPhone"></el-table-column>
      <el-table-column label="省" prop="provinceName"></el-table-column>
      <el-table-column label="市" prop="cityName"></el-table-column>
      <el-table-column label="县/市/区" prop="districtName"></el-table-column>
      <el-table-column label="详细地址" prop="address"></el-table-column>
      <el-table-column label="创建时间" prop="creationTime"></el-table-column>
      <el-table-column label="操作人" prop="realName"></el-table-column>
      <el-table-column fixed="right" label="操作" width="100">
        <template slot-scope="scope">
          <el-button type="text" size="small" @click="onEdit(scope.row)">编辑</el-button>
        </template>
      </el-table-column>
    </el-table>
    <div>
      <el-pagination
        background
        layout="prev, pager, next"
        :total="total"
        :page-size="form.pageSize"
        @current-change="currentChange"
      ></el-pagination>
    </div>
    <el-dialog
      title="新增供应商"
      style="z-index:2"
      :visible.sync="dialogAddSupplier"
      width="30%"
      :close-on-click-modal="false"
      :destroy-on-close="true"
    >
      <AddSupplier v-if="dialogAddSupplier" />
    </el-dialog>

    <el-dialog
      title="编辑供应商"
      style="z-index:2"
      :visible.sync="dialogEditSupplier"
      width="30%"
      :close-on-click-modal="false"
      :destroy-on-close="true"
    >
      <EditSupplier v-if="dialogEditSupplier" />
    </el-dialog>

    <el-dialog
      :width="imgdialogWidth"
      :visible.sync="imgdialogVisible"
      :close-on-click-modal="false"
      :modal="true"
      :destroy-on-close="true"
    >
      <img :src="imageUrl" v-if="imgdialogVisible" v-on:load.stop="imgOnload" alt />
    </el-dialog>
  </div>
</template>
<script>
import VueHoverMask from "vue-hover-mask";
import http from "../../common/http/vueresource.js";
import AddSupplier from "./AddSupplier.vue";
import EditSupplier from "./EditSupplier.vue";
export default {
  data() {
    return {
      total: 0,
      dialogAddSupplier: false,
      dialogEditSupplier: false,
      imgdialogWidth: "0px",
      imgdialogVisible: false,
      imageUrl: "",
      form: {
        pageIndex: 0,
        pageSize: 10,
        likeSupplierName: "",
        proCode: "",
        contactMan: "",
        contactPhone: "",
        startTime: "",
        endTime: ""
      },
      tableData: []
    };
  },
  components: {
    VueHoverMask,
    AddSupplier,
    EditSupplier
  },
  mounted() {
    this.getData();
  },
  methods: {
    currentChange(pageIndex) {
      this.form.pageIndex = pageIndex;
      this.getData();
    },
    imgOnload(e) {
      if (e.path && e.path[0] && e.path[0].nodeName == "IMG") {
        this.imgdialogWidth = e.path[0].width + 40 + "px";
      }
    },
    handlePicturePreview(e) {
      console.log(e);
      if (e.target.dataset && e.target.dataset.sourceimgs) {
        this.imageUrl = http.rootApi + e.target.dataset.sourceimgs;
        this.imgdialogVisible = true;
      }
    },
    getThumUrl(thums) {
      if (thums && thums.length > 0) {
        return http.rootApi + thums[0];
      }
      return "";
    },
    getData() {
      //请求查询数据
      var api = "/api/Supplier/SearchPageList";
      http.get(api, JSON.stringify(this.form), response => {
        if (response && response.success && response.data) {
          //绑定列表
          this.tableData = response.data.items;
          //总记录数
          this.total = response.data.totalCount;
        } else {
          this.tableData.clear();
        }
      });
    },
    onSearch() {},
    onEdit(row) {
      this.currentId = row.id;
      this.dialogEditSupplier = true;
    }
  }
};
</script>